using Newtonsoft.Json;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Model;
using System.Net.Http.Headers;
using System.Text;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey
{
    public class CheckMoveAvailabilityService : ICheckMoveAvailabilityService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IToJourneyKeyService _toJourneyKeyService;
        private readonly IModelService _modelService;
        private readonly IConfiguration _configuration;
        public CheckMoveAvailabilityService(HttpClient httpClient, ILogHelper logHelper, IToJourneyKeyService toJourneyKeyService, IModelService modelService, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logHelper = logHelper;
            _toJourneyKeyService = toJourneyKeyService;
            _modelService = modelService;
            _configuration = configuration;
        }
        public async Task<ToJourneyDetails?> ToJourneyKey(Reaccommodation_Model? reaccommodationMs, string token)
        {
            ToJourneyDetails? toJourneyDetails = null;
            try
            {
                if (reaccommodationMs?.bookingDetails?.Data?.Passengers != null && reaccommodationMs?.bookingDetails?.Data?.Passengers.Count() > 0)
                {
                    reaccommodationMs.PassengersCount = reaccommodationMs.bookingDetails.Data.Passengers.Count;
                    var moveavailability = await GetTripmoveavailability(reaccommodationMs.bookingDetails, token);
                    if (moveavailability != null && moveavailability != "")
                    {
                        reaccommodationMs.tripMoveAvailability = JsonConvert.DeserializeObject<TripMoveAvailability>(moveavailability);
                        if (reaccommodationMs.tripMoveAvailability != null && reaccommodationMs.tripMoveAvailability.Data != null && reaccommodationMs.tripMoveAvailability.Data.Results != null && reaccommodationMs.tripMoveAvailability.Data.Results.Any())
                        {
                            toJourneyDetails = await fnKeyJourneyNavigateFlightDate(reaccommodationMs);
                        }
                    }
                    return toJourneyDetails;
                }
                else
                {
                    await _logHelper.LogError($"{ToJourneyKey}{":- PNR :- "}{reaccommodationMs?.PNRDetail?.PNRCode}{"-:-Check the PNR"}");
                    return null;    
                }
                    
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"CheckMoveAvailabilityService:-"}{"ToJourneyKey PNR " + reaccommodationMs?.PNRDetail?.PNRCode ?? ""} :- {"End msg :" + ex.Message}");
                await _logHelper.LogConsoleException(ex);
                return toJourneyDetails;
            }
        }
        private async Task<string> GetTripmoveavailability(BookingDetails bookingDetailsModel, string Token)
        {
            try
            {
                TripMoveAvailabilityRequest moveavailability = await _modelService.GetMoveavailabilityRequest(bookingDetailsModel, 0);
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(moveavailability);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                    _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                var response = await _httpClient.PostAsync("", content);
                var result = await response.Content.ReadAsStringAsync();
                return result;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"CheckMoveAvailabilityService:-"}{"GetTripmoveavailability"} :- {"End msg :" + ex.Message}");
                await _logHelper.LogConsoleException(ex);
                return "";
            }
        }
        // obtain the Journey Flight date  
        private async Task<ToJourneyDetails?> fnKeyJourneyNavigateFlightDate(Reaccommodation_Model? reaccommodationMs)
        {
            try
            {
                ToJourneyDetails? toJourneyDetails = await _toJourneyKeyService.GetKeyJourneyNavigateFlightDate(reaccommodationMs,false);
                return toJourneyDetails;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"CheckMoveAvailabilityService:-"}{"fnKeyJourneyNavigateFlightDate PNR " + reaccommodationMs?.PNRDetail?.PNRCode ?? ""} :- {"End msg :" + ex.Message}");
                return null;
            }
        }
    }
}
