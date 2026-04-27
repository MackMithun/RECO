using Newtonsoft.Json;
using RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.DistrubtionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.IUtilities;
using RECO.DistrubtionHandler_MS.Models.DatabaseModel;
using RECO.DistrubtionHandler_MS.Models.Enum;
using RECO.DistrubtionHandler_MS.Models.RequestModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel;
using System.Text;

namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService
{
    public class ReaccommodationMSService : IReaccommodationMSService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        private readonly IHandleDisruptedFlightService _handleDisruptedFlightService;
        public ReaccommodationMSService(IHandleDisruptedFlightService handleDisruptedFlightService, HttpClient httpClient, ILogHelper logHelper, IConfiguration configuration)
        {
            _handleDisruptedFlightService = handleDisruptedFlightService;
            _httpClient = httpClient;
            _logHelper = logHelper;

            _configuration = configuration;
        }
        public async Task<IdentifierResponse> GetFlightDetails(FlightData flightData, string token)
        {
            try
            {
                IdentifierResponse identifierResponse = new IdentifierResponse();
                identifierResponse.data = new FlightDetails();
                identifierResponse.isSuccess = true;
                identifierResponse.message = Enum_CustomMessage.Successful.ToString();
                identifierResponse.data =  await _handleDisruptedFlightService.GetFlightDetails(flightData, token);
                identifierResponse.errorMessage = null;
                return identifierResponse;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(Enum_CustomMessage.InvalidFlight.GetDescription());
            }
        }
        public async Task<bool> RetryRECO(DisruptedFlight disruptedFlight)
        {
            try
            {
                RecoFlight recoFlight = await _handleDisruptedFlightService.CheckEligibility(disruptedFlight);
                IntiateReaccommodation(disruptedFlight, recoFlight.exceptionFlightList?.AlternateFlightNumber ?? "");
                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- RetryRECO"} :- {ex?.Message??""}");
                return false;
            }
        }
        public async Task<bool> InitiateReco(DisruptedFlight disruptedFlight)
        {
            try
            {
                bool InitiateStatus = false;
                int retryCount = 0;
                int maxRetries = 2;

                while (!InitiateStatus && retryCount < maxRetries)
                {
                    InitiateStatus = await fnInitiateReco(disruptedFlight);
                    retryCount++;
                }
                if (!InitiateStatus)
                {
                    _logHelper.LogInfo($"{"Failed to initiate reco after 2 attempts."}{":- Flight No :- "}{disruptedFlight.Identifier}");
                    throw new InvalidOperationException("Failed to initiate reco after 2 attempts.");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- InitiateReco"} :- {ex?.Message ?? ""}");
                throw new InvalidOperationException(ex?.Message??"");
            }
        }
        public async Task<bool> fnInitiateReco(DisruptedFlight disruptedFlight)
        {
            try
            {
                RecoFlight recoFlight = await _handleDisruptedFlightService.CheckEligibility(disruptedFlight);
                IntiateReaccommodation(disruptedFlight, recoFlight.exceptionFlightList?.AlternateFlightNumber ?? "");
                return true;
            }
            catch (Exception ex)
            {
                if(ex.Message == Enum_CustomMessage.Reaccommodationcriteria.GetDescription() || ex.Message == Enum_CustomMessage.FLtNotUpdatedinNav.ToString() ||
                    ex.Message.Contains("already in action of the specified date") || ex.Message == Enum_CustomMessage.noactionrequired.GetDescription() ||
                    ex.Message == Enum_CustomMessage.approvalisrequired.GetDescription() || ex.Message == Enum_CustomMessage.WaitingForApproval.ToString() || ex.Message== Enum_CustomMessage.NoActionRequired.ToString() || ex.Message == Enum_CustomMessage.OutsideEligibilityWindow.ToString())
                {
                    throw new InvalidOperationException(ex.Message);
                }
                return false;
            }
        }
        private ReAccommodationByFlightNoRequestModel MapReAccommodationByFlightNo(DisruptedFlight disruptedFlight, string identifier)
        {
            ReAccommodationByFlightNoRequestModel reAccommodationByFlightNoRequestModel = new ReAccommodationByFlightNoRequestModel
            {
                origin = disruptedFlight?.OriginStations?[0],
                destination = disruptedFlight?.DestinationStations?[0],
                carrierCode = disruptedFlight?.CarrierCode,
                beginDate = disruptedFlight?.BeginDate,
                oldIdentifier = disruptedFlight?.Identifier,
                flightType = disruptedFlight.FlightType,
                newIdentifier = identifier,
            };
            return  reAccommodationByFlightNoRequestModel;
        }
        private HttpResponseMessage IntiateReaccommodation(DisruptedFlight disruptedFlight, string identifier)
        {
            ReAccommodationByFlightNoRequestModel tripInfo =  MapReAccommodationByFlightNo(disruptedFlight, identifier);
            string jsonBody = JsonConvert.SerializeObject(tripInfo);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("GatewayKey"));
            var result = _httpClient.PostAsync(_configuration.GetSection("InternalMSAPI")["RecoReaccommodationMS1"], content);
            return result.Result;
        }

        public async Task<List<DisruptedFlight>> cronJobSchedule()
        {
            try
            {
                _logHelper.LogInfo("*******  Start cronJobSchedule ******** ");
                List<DisruptedFlight> disruptedFlights =await _handleDisruptedFlightService.DisruptedFlight();
                string RetryFlight = disruptedFlights != null && disruptedFlights.Any() ? string.Join(",", disruptedFlights.Where(x => x.Identifier != null).Select(x => x.Identifier)): string.Empty;
                _logHelper.LogInfo("*******  End cronJobSchedule ******** disruptedFlights Count : "+ disruptedFlights?.Count()??"");
                return disruptedFlights;    
            }
            catch (Exception ex) {
                _logHelper.LogConsoleException(ex);
                return new List<DisruptedFlight>();
            }
        }
    }
}
