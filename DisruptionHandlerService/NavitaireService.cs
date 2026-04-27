using Newtonsoft.Json;
using RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.DistrubtionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.IUtilities;
using RECO.DistrubtionHandler_MS.Models.Enum;
using RECO.DistrubtionHandler_MS.Models.RequestModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel.ManifestModel;
using System.Net.Http.Headers;
using System.Text;

namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService
{
    public class NavitaireService : INavitaireService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        private readonly IAuthService _authorizationService;

        public NavitaireService(HttpClient httpClient, ILogHelper logHelper, IConfiguration configuration, IAuthService authorizationService)
        {
            _httpClient = httpClient;
            _logHelper = logHelper;
            _configuration = configuration;
            _authorizationService = authorizationService;
        }
        public async Task<List<DateTime>> VerifySTDInNavitaire(NavitaireFlightRequest navitaireFlightRequest)
        {
            try
            {
                List<DateTime> designator = new List<DateTime>();
                string ? token = await _authorizationService.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    var jsonPayload = System.Text.Json.JsonSerializer.Serialize(navitaireFlightRequest);
                    HttpResponseMessage response = await HTTPMethod(token, jsonPayload);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    TripinfoLegListModel? tripInfoLegsResponseModel = JsonConvert.DeserializeObject<TripinfoLegListModel>(responseContent);
                    if (tripInfoLegsResponseModel != null && tripInfoLegsResponseModel.data != null && tripInfoLegsResponseModel.data.Count() > 0)
                    {
                        foreach (var segment in tripInfoLegsResponseModel.data[0].journeys)
                        {
                            segment.segments.RemoveAll(segment => segment?.identifier?.identifier != navitaireFlightRequest.Identifier);
                        }
                        designator.Add((DateTime)tripInfoLegsResponseModel.data[0]?.journeys?[0].designator?.departure);
                        designator.Add((DateTime)tripInfoLegsResponseModel.data[0]?.journeys?[0].designator?.arrival);
                        return designator;
                    }
                    else
                    {
                        _logHelper.LogInfo($"{" :- VerifySTDInNavitaire"} :- {JsonConvert.SerializeObject(navitaireFlightRequest)}");
                        throw new InvalidOperationException(Enum_CustomMessage.TripNotAvailable.GetDescription());
                    }
                }
                throw new InvalidOperationException(Enum_CustomMessage.NavitaireFailure.GetDescription());
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- VerifySTDInNavitaire"} :- {ex?.Message??""} :- {JsonConvert.SerializeObject(navitaireFlightRequest)}");
                throw new InvalidOperationException(ex?.Message== Enum_CustomMessage.TripNotAvailable.GetDescription() ? ex.Message: Enum_CustomMessage.NavitaireFailure.GetDescription());
            }
        }

        public async Task<ListOfHandleDiscruption> GetNavitaireFlight(NavitaireFlightRequest tripInfo, Dictionary<string, string> appParameterTable)
        {
            string? token = await _authorizationService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(tripInfo);
                HttpResponseMessage response = await HTTPMethod(token, jsonPayload);
                string responseContent = await response.Content.ReadAsStringAsync();
                TripinfoLegListModel? tripInfoLegsResponse = JsonConvert.DeserializeObject<TripinfoLegListModel>(responseContent);
                if (tripInfoLegsResponse != null && tripInfoLegsResponse.data != null && tripInfoLegsResponse.data.Any())
                {
                    foreach (var segment in tripInfoLegsResponse.data[0].journeys)
                    {
                        segment.segments.RemoveAll(segment => segment?.identifier?.identifier != tripInfo.Identifier);
                    }

                   var  listOfHandle = await checkTheNavitaireStatus(appParameterTable, tripInfoLegsResponse, token);

                    return listOfHandle;
                }
            }
            return null;

        }
        private async Task<ListOfHandleDiscruption> checkTheNavitaireStatus( Dictionary<string, string> appParameterTable, TripinfoLegListModel? tripInfoLegsResponseModel, string Token)
        {
            try
            {
                ListOfHandleDiscruption legKeyStatusFlight = new ListOfHandleDiscruption();
                LegKeyStatusFlightInformationModel legKeyStatusFlightInformationModel = new LegKeyStatusFlightInformationModel();
                List<string> _ListofLegKey = await GetLegKeyManifest(tripInfoLegsResponseModel);
                //check the Satus to navitaire
                foreach(string legkey in _ListofLegKey)
                {
                    legKeyStatusFlightInformationModel = await GetLegKeyStatus(appParameterTable, legkey, Token);
                    if(legKeyStatusFlightInformationModel.status == Enum_DisruptionType.cancelled|| legKeyStatusFlightInformationModel.status == Enum_DisruptionType.delayed|| legKeyStatusFlightInformationModel.status == Enum_DisruptionType.advanced)
                    {
                        break;  
                    }
                }
                legKeyStatusFlight.legKeyStatusFlightInformationModel = legKeyStatusFlightInformationModel;
                legKeyStatusFlight._DisruptionType = legKeyStatusFlightInformationModel.status;
                return legKeyStatusFlight;

            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- VerifySTDInNavitaire"} :- {ex?.Message ?? ""}");
                throw new InvalidOperationException(Enum_CustomMessage.CheckNavitaireStatus.GetDescription());
            }
        }
        public async Task<LegKeyStatusFlightInformationModel> GetLegKeyStatus(Dictionary<string, string> appParameterTable, string legKey, string token)
        {
            try
            {
                LegKeyStatusFlightInformationModel legKeyStatusFlightInformationModel = new LegKeyStatusFlightInformationModel();

                if (legKey != "" && legKey != null)
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{legKey}/status");
                    if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                        _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    var response = await _httpClient.SendAsync(request);
                    string result = await response.Content.ReadAsStringAsync();
                    LegKeyStatusResponseModel? legKeyStatusResponse = JsonConvert.DeserializeObject<LegKeyStatusResponseModel>(result);
                    if (legKeyStatusResponse != null)
                    {
                        int? _statusCode = legKeyStatusResponse?.Data?.OperationDetails?.Status;
                        legKeyStatusFlightInformationModel.scheduledDepartureTime = legKeyStatusResponse?.Data?.OperationDetails?.TripOperationTimes?.DepartureTimes?.Scheduled;
                        legKeyStatusFlightInformationModel.scheduledArrivalTime = legKeyStatusResponse?.Data?.OperationDetails?.TripOperationTimes?.ScheduledArrivalTime;
                        if (_statusCode == 2)
                        {
                            legKeyStatusFlightInformationModel.status = Enum_DisruptionType.cancelled;
                        }
                        else if (legKeyStatusResponse?.Data?.OperationDetails?.TripOperationTimes?.TouchDownTimes.Estimated != null)
                        {
                            StatusUpdate(appParameterTable, legKeyStatusFlightInformationModel, legKeyStatusResponse);
                        }
                        else
                        {
                            legKeyStatusFlightInformationModel.status = Enum_DisruptionType.FLtNotUpdatedinNav;
                        }

                    }
                }
                return legKeyStatusFlightInformationModel;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- GetLegKeyStatus"} :- {ex?.Message ?? ""}");
                throw new InvalidOperationException(Enum_CustomMessage.NavitaireFailure.GetDescription());
            }
        }

        private void StatusUpdate(Dictionary<string, string> appParameterTable, LegKeyStatusFlightInformationModel legKeyStatusFlightInformationModel, LegKeyStatusResponseModel? legKeyStatusResponse)
        {
            legKeyStatusFlightInformationModel.EstimateDepartureTime = legKeyStatusResponse.Data.OperationDetails?.TripOperationTimes?.DepartureTimes?.Estimated;
            legKeyStatusFlightInformationModel.EstimateArrivalTime = legKeyStatusResponse.Data.OperationDetails?.TripOperationTimes?.TouchDownTimes?.Estimated;

            //Estimate a minimum delay of 2 hours and a minimum advancement of 1 hour.
            TimeSpan delayThreshold = TimeSpan.FromMinutes(Convert.ToInt32(appParameterTable[Enum_AppParameters.MinimumDelay.ToString()]));
            TimeSpan advancementThreshold = TimeSpan.FromMinutes(Convert.ToInt32(appParameterTable[Enum_AppParameters.PreponMin.ToString()]));
            //Postpond
            if (legKeyStatusFlightInformationModel.EstimateDepartureTime > legKeyStatusFlightInformationModel.scheduledDepartureTime + delayThreshold)
            {
                legKeyStatusFlightInformationModel.status = Enum_DisruptionType.delayed;
            }
            //prepond
            else if (legKeyStatusFlightInformationModel.EstimateDepartureTime < legKeyStatusFlightInformationModel.scheduledDepartureTime - advancementThreshold)
            {
                legKeyStatusFlightInformationModel.status = Enum_DisruptionType.advanced;
            }
            else
            {
                legKeyStatusFlightInformationModel.status = Enum_DisruptionType.FLtNotUpdatedinNav;
            }
        }

        // return the 
        // return the Leg  VIA flights
        private Task<List<string>> GetLegKeyManifest(TripinfoLegListModel? tripInfoLegsResponseModel)
        {
            try
            {
                HashSet<string> setofLegKey = new HashSet<string>();
                List<string> finalLegKey = new List<string>();
                if (tripInfoLegsResponseModel.data[0].journeys.Count > 0)
                {
                    foreach (var journey in tripInfoLegsResponseModel?.data?[0].journeys)
                    {
                        foreach (var segments in journey.segments)
                        {
                            foreach(var leg in segments.legs)
                            {
                                setofLegKey.Add(leg.legKey);
                            }
                        }
                    }
                    finalLegKey.AddRange(setofLegKey);
                }
                else
                {
                    throw new InvalidOperationException(Enum_CustomMessage.CheckleyKey.FormatMessage(tripInfoLegsResponseModel?.data?[0].journeys?[0].segments?[0].identifier?.ToString() ?? ""));
                }
                return Task.FromResult(finalLegKey);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- GetLegKeyStatus"} :- {ex?.Message ?? ""}:{tripInfoLegsResponseModel?.data?[0].journeys?[0].segments?[0].identifier?.ToString()??""}");
                throw new InvalidOperationException(Enum_CustomMessage.LegKeyManifest.GetDescription());
            }
        }
        private async Task<HttpResponseMessage> HTTPMethod(string? Token, string jsonPayload)
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
            var response = await _httpClient.PostAsync("", content);
            var result = await response.Content.ReadAsStringAsync();
            return response;
        }
    }
}
