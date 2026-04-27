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
    public class MoveAvailabilityServiceForNextDays : IMoveAvailabilityServiceForNextDays
    {
        private readonly ILogHelper _logHelper;
        private readonly HttpClient _httpClient;
        private readonly IModelService _modelService;
        private readonly IConfiguration _configuration;
        private readonly IToJourneyKeyService _toJourneyKeyService;
        public MoveAvailabilityServiceForNextDays(ILogHelper logHelper, IToJourneyKeyService toJourneyKeyService, HttpClient httpClient, IConfiguration configuration,
            IModelService modelService)
        {
            _logHelper = logHelper;
            _toJourneyKeyService = toJourneyKeyService;
            _httpClient = httpClient;
            _modelService = modelService;
            _configuration = configuration;
        }
        public async Task<ToJourneyDetails?> ToJourneyKey(Reaccommodation_Model? reaccommodationMs, string token)
        {
            try
            {
                if(reaccommodationMs?.bookingDetails?.Data?.Passengers!=null && reaccommodationMs?.bookingDetails?.Data?.Passengers.Count()>0)
                {
                    reaccommodationMs.PassengersCount = reaccommodationMs.bookingDetails.Data.Passengers.Count;
                    ToJourneyDetails? toJourneyDetails = await fnKeyJourneyNavigate7Days(reaccommodationMs, token);
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
                await _logHelper.LogError($"{"ToJourneyKey PNR : " + reaccommodationMs?.PNRDetail?.PNRCode ?? ""} :- {"End msg =" + ex.Message}");
                await _logHelper.LogConsoleException(ex);
                return null;
            }
        }
        //Check for flight availability for the PNR for more than 7 days.
        private async Task<ToJourneyDetails?> fnKeyJourneyNavigate7Days(Reaccommodation_Model? reaccommodationMs, string token)
        {
            try
            {
                ToJourneyDetails toJourneyDetails = null;
                reaccommodationMs.tripMoveAvailability = new TripMoveAvailability();   
                int CheckNextForAvailability = await fnCheckNextForAvailability(reaccommodationMs);
                int Days = 0;

                //first go to Zero
                reaccommodationMs.tripMoveAvailability = await GetCheckMoreAvailabilityFlight(reaccommodationMs, 0, token);
                toJourneyDetails = await _toJourneyKeyService.GetKeyJourneyNavigateFlightDate(reaccommodationMs,true);

                while (Days <= CheckNextForAvailability && (toJourneyDetails == null || string.IsNullOrEmpty(toJourneyDetails.JourneyKey)))
                {
                    reaccommodationMs.tripMoveAvailability = await GetCheckMoreAvailabilityFlight(reaccommodationMs, Days, token);
                    toJourneyDetails = await _toJourneyKeyService.GetKeyJourneyNavigateFlightDate(reaccommodationMs,true);
                    Days++;
                }
                int CheckPriorForAvailability = await fnCheckPriorForAvailability(reaccommodationMs);
                Days = -1;
                while (CheckPriorForAvailability <= Days && (toJourneyDetails == null || string.IsNullOrEmpty(toJourneyDetails.JourneyKey)))
                {
                    reaccommodationMs.tripMoveAvailability = await GetCheckMoreAvailabilityFlight(reaccommodationMs, Days, token);
                    toJourneyDetails = await _toJourneyKeyService.GetKeyJourneyNavigateFlightDate(reaccommodationMs,true);
                    Days--; 
                }
                return toJourneyDetails;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"KeyJourneyNavigate7Days PNR : " + reaccommodationMs?.PNRDetail?.PNRCode ?? ""} :- {"End msg =" + ex.Message}");
                return null;
            }
        }
        private async Task<int> fnCheckPriorForAvailability(Reaccommodation_Model? reaccommodationMs)
        {
            try
            {
                int CheckPriorForAvailability = Convert.ToInt32(reaccommodationMs?.AppParameterList?["CheckPriorForAvailability"]);
                int Flag = CheckPriorForAvailability;
                DateTime? FlightDate = (DateTime)reaccommodationMs?.bookingDetails?.Data?.Journeys?[0].Designator.Departure;
                if (FlightDate.HasValue)
                {
                    FlightDate = FlightDate.Value;
                }
                DateTime? currentDateTime = TimeZoneService.convertToIND(DateTime.UtcNow);
                TimeSpan? difference = FlightDate - currentDateTime;
                int differenceInDays = (int)difference?.TotalDays;
                if (differenceInDays >= 1 && differenceInDays <=(-(CheckPriorForAvailability)))
                {
                    Flag = -differenceInDays;
                }
                else
                {
                    if(CheckPriorForAvailability<=0)
                    {
                        Flag = 1;
                    }
                }
                if (reaccommodationMs.impactedFlight.ArrivalStationCode != null)
                {
                    DateTime FlightDateTime = (DateTime)reaccommodationMs?.bookingDetails?.Data?.Journeys?[0].Designator.Departure;
                    DateTime NearestEffectDateTime = (DateTime)reaccommodationMs.impactedFlight.NearestJourneySAT;
                    TimeSpan fdifference = FlightDateTime - NearestEffectDateTime;
                    int fdifferenceInDays = (int)fdifference.TotalDays;
                    if(fdifferenceInDays < 1)
                    {
                        if (CheckPriorForAvailability <= 0)
                        {
                            Flag = 1;
                        }
                    }
                    else if (fdifferenceInDays >= 1 && fdifferenceInDays <= (-(CheckPriorForAvailability)))
                    {
                        Flag = -fdifferenceInDays;
                    }
                }
                return Flag;   
            }
            catch (Exception ex) {
                await _logHelper.LogError($"{"fnCheckPriorForAvailability : "}{ex.Message}");
                await _logHelper.LogConsoleException(ex); 
                return 1;
            }
        }
        private async Task<int> fnCheckNextForAvailability(Reaccommodation_Model? reaccommodationMs)
        {
            try
            {
                int CheckNextForAvailability = Convert.ToInt32(reaccommodationMs?.AppParameterList?["CheckNextForAvailability"]);
                if (reaccommodationMs.impactedFlight.DepartureStationCode != null)
                {
                    DateTime FlightDateTime = (DateTime)reaccommodationMs?.bookingDetails?.Data?.Journeys?[0].Designator.Arrival;
                    DateTime NearestEffectDateTime = (DateTime)reaccommodationMs.impactedFlight.NearestJourneySDT;
                    TimeSpan difference = NearestEffectDateTime - FlightDateTime;
                    int differenceInDays = (int)difference.TotalDays;
                    if (differenceInDays < 7)
                    {
                        CheckNextForAvailability = differenceInDays;
                    }
                }
                return CheckNextForAvailability;
            }
            catch (Exception ex) {
                await _logHelper.LogError($"{"fnCheckNextForAvailability : "}{ex.Message}");
                await _logHelper.LogConsoleException(ex);
                return 1;
            }
        }

        private async Task<TripMoveAvailability?> GetCheckMoreAvailabilityFlight(Reaccommodation_Model? reaccommodationMs, int daysCounter, string Token)
        {
            try
            {
                reaccommodationMs.tripMoveAvailability = await fnCheckMoreAvailabilityFlight(reaccommodationMs.bookingDetails, daysCounter, Token);
                if (reaccommodationMs?.tripMoveAvailability == null || reaccommodationMs.tripMoveAvailability.Data == null || reaccommodationMs.tripMoveAvailability.Data?.Results?.Count == 0)
                {
                    //"Flight segments are not available for up to 7 days."
                    return null;
                }
                return reaccommodationMs.tripMoveAvailability;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"GetCheckMoreAvailabilityFlight PNR : " + reaccommodationMs?.PNRDetail?.PNRCode ?? ""} :- {"End msg =" + ex.Message}");
                await _logHelper.LogConsoleException(ex);
                return null;
            }
        }
        //Check for flight availability for the PNR for more than 7 days.
        private async Task<TripMoveAvailability> fnCheckMoreAvailabilityFlight(BookingDetails? bookingDetailsModel, int NextAddDays, string Token)
        {
            TripMoveAvailability moveavailabilityResponseModel = new TripMoveAvailability();
            try
            {
                TripMoveAvailabilityRequest moveavailability = await _modelService.GetMoveavailabilityRequest(bookingDetailsModel, NextAddDays);
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(moveavailability);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                    _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                var response = await _httpClient.PostAsync("", content);
                var result = await response.Content.ReadAsStringAsync();
                moveavailabilityResponseModel = JsonConvert.DeserializeObject<TripMoveAvailability>(result);
                return moveavailabilityResponseModel;
            }
            catch (Exception ex)
            {
                await _logHelper.LogConsoleException(ex);
                return moveavailabilityResponseModel;
            }

        }

    }
}
