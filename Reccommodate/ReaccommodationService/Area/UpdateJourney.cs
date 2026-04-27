using Microsoft.Extensions.Options;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.Models.Enum;
using RECO.Reaccommodation_MS.Models.ResponseModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Model;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area
{
    public class UpdateJourney : IUpdateJourney
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IModelService _modelService;
        private readonly QueueModel _queueModel;
        private readonly IConfiguration _configuration;
        private readonly IJourneyService _journeyService;
        private readonly string _caller = typeof(UpdateJourney).Name;
        public UpdateJourney(HttpClient httpClient, ILogHelper logHelper, IModelService modelService, 
            IOptions<QueueModel> queueModel, IConfiguration configuration, IJourneyService journeyService)
        {
            _httpClient = httpClient;
            _logHelper = logHelper;
            _modelService = modelService;
            _queueModel = queueModel.Value;
            _configuration = configuration;
            _journeyService = journeyService;       
        }
        public async Task<PNRDetails> SetUpdateJourney(Reaccommodation_Model? reaccommodation_Model, string token)
        {
            try
            {
                JourneyRequestModel journeyRequest = new JourneyRequestModel
                {
                    FromJourneykey = reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].JourneyKey,
                    Farekey = reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].Segments?[0].Fares?[0].FareKey,
                    ToJourneykey = reaccommodation_Model?.ToJourneyDetail?.JourneyKey,
                    BookingComment = _queueModel.BookingComment
                };
                UpdateJourneyModel? request = await _modelService.UpdateMoveJourneyRequest(journeyRequest);
                if (request != null)
                {
                    return await updateTheJourney(reaccommodation_Model, request, token);
                }
                else
                {
                    await _logHelper.LogInfo($"{_caller}:{"SetUpdateJourney"}{" :-PNR:- "}{reaccommodation_Model?.PNRDetail?.PNRCode??""} :- {" :-end"}");
                    return await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodation_Model?.bookingDetails, null, reaccommodation_Model?.PNRDetail, false, Enum_PNR.UpdateMoveJourneyFailed.ToString());
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{"SetUpdateJourney"}{" :-PNR:- "}{reaccommodation_Model?.PNRDetail?.PNRCode ?? ""} :- {ex.Message}{" :-end"}");
                await _logHelper.LogConsoleException(ex);
                return await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodation_Model?.bookingDetails, null, reaccommodation_Model?.PNRDetail, false, Enum_PNR.NotAbleToFetchDetails.ToString()); ;

            }
        }
        private async Task<PNRDetails> updateTheJourney(Reaccommodation_Model? reaccommodation_Model, UpdateJourneyModel? request, string token)
        {
            try
            {
                string OLDFlightNo = reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].Segments?[0].Identifier?.Identifier ?? "";
                DateTime OLDFlightDate = reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].Segments?[0].Designator?.Departure?? new DateTime();
                BookingDetails? newBooking = new BookingDetails();
                var jsonPayload = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                    _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await _logHelper.LogInfo(reaccommodation_Model?.PNRDetail?.PNRCode + " : " + jsonPayload);
                var response = await _httpClient.PutAsync($"{reaccommodation_Model?.PNRDetail?.PNRCode}/journey", content);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    await _logHelper.LogInfo(reaccommodation_Model?.PNRDetail?.PNRCode + " : " + response.StatusCode);
                    await _logHelper.LogInfo($"{_caller}:{"SetUpdateMultipleJourneyFlightDate"} :- {"UpdateMoveJourneyRequest success"}");
                    newBooking = await CommitTheBooking(reaccommodation_Model, OLDFlightNo, OLDFlightDate, token);
                    if (newBooking != null)
                    {
                        return await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodation_Model?.EntireBooking, newBooking, reaccommodation_Model?.PNRDetail, true, Enum_PNR.Completed.ToString());
                    }
                    return await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodation_Model?.bookingDetails, null, reaccommodation_Model?.PNRDetail, false, Enum_PNR.UpdateMoveJourneyFailed.ToString());
                }
                await _logHelper.LogInfo($"{reaccommodation_Model?.PNRDetail?.PNRCode}:{"SetUpdateMultipleJourneyFlightDate"} :- {"end"}" + response.StatusCode);
                return await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodation_Model?.bookingDetails, null, reaccommodation_Model?.PNRDetail, false, Enum_PNR.UpdateMoveJourneyFailed.ToString());

            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{"updateTheJourney"}{" PNR : "}{reaccommodation_Model?.PNRDetail?.PNRCode ?? ""} :- {ex.Message}");
                await _logHelper.LogConsoleException(ex);
                return await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodation_Model?.bookingDetails, null, reaccommodation_Model?.PNRDetail, false, Enum_PNR.NotAbleToFetchDetails.ToString()); ;
            }
        }
        private async Task<BookingDetails?> CommitTheBooking(Reaccommodation_Model? reaccommodation_Model, string OLDFlightNo, DateTime OLDFlightDate, string token)
        {
            try
            {
                var newbookingDetails = await reaccommodation_Model.bookingService.GetBookingByRecordLocator(reaccommodation_Model?.PNRDetail?.PNRCode??"", token);
                if (newbookingDetails != null && newbookingDetails != "")
                {
                    BookingDetails? _BookingDetails = Newtonsoft.Json.JsonConvert.DeserializeObject<BookingDetails>(newbookingDetails);
                    if (_BookingDetails != null && _BookingDetails.Data != null && _BookingDetails?.Data?.Journeys?.Count > 0)
                    {
                        bool newFlightStatus = _BookingDetails.Data?.Journeys?.Any(journey => journey.Segments.Any(segment => segment?.Identifier?.Identifier == OLDFlightNo && segment?.Designator?.Arrival== OLDFlightDate)) ?? false;
                        await _logHelper.LogInfo($"{reaccommodation_Model?.PNRDetail?.PNRCode}:{"CommitTheBooking"} :- {"Start "} PNR :-" + reaccommodation_Model?.PNRDetail?.PNRCode ?? "" + " OLDFlightNo=" + OLDFlightNo + ", newFlightStatus=" + newFlightStatus);
                        if (!newFlightStatus)
                        {
                            string CommitComment = _queueModel.CommitComment;
                            string? oldQueue = _queueModel.oldQueue;
                            bool status = await reaccommodation_Model.queueService.fnDeleteQueue(oldQueue, token);
                            await _logHelper.LogInfo($"{"fnDeleteQueue"} :- PNR :-" + reaccommodation_Model?.PNRDetail?.PNRCode ?? "" + " status=" + status);
                            //await multiJourney_Model.QueueService.fnCreateQueue(token);
                            bool Commitresponse = await reaccommodation_Model.bookingCommit.SetBookingallowConcurrentChanges(token, CommitComment);
                            await _logHelper.LogInfo($"{"SetBookingallowConcurrentChanges"} :- PNR :-" + reaccommodation_Model?.PNRDetail?.PNRCode ?? "" + " Commitresponse=" + Commitresponse);
                            return _BookingDetails;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{"CommitTheBooking"}{" PNR : "}{reaccommodation_Model?.PNRDetail?.PNRCode ?? ""} :- {ex.Message}");
                return null;
            }
        }
        
    }
}
