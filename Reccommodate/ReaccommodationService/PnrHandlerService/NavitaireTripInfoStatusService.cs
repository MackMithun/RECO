using Newtonsoft.Json;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.Enum;
using RECO.Reaccommodation_MS.Models.ResponseModel.LegModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.ManifestModel;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;
using System.Net.Http.Headers;
using System.Text;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class NavitaireTripInfoStatusService : INavitaireTripInfoStatusService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        public NavitaireTripInfoStatusService(HttpClient httpClient, ILogHelper logHelper, IConfiguration configuration)
        {

            _httpClient = httpClient;
            _logHelper = logHelper;
            _configuration = configuration; 
        }
        public async Task<LegKeyStatusFlightInformationModel> GetLegKeyStatusAsync(Dictionary<string, string>  appParameterTable, string LegKey, string Token)
        {
            try
            {
                LegKeyStatusFlightInformationModel legKeyStatusFlightInformationModel = new LegKeyStatusFlightInformationModel();
                legKeyStatusFlightInformationModel.status = Enum_DisruptionType.NotPostPonedNorCancelled.ToString();

                if (LegKey != "" && LegKey != null)
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{LegKey}/status");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                    if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                        _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
                    var response = await _httpClient.SendAsync(request);
                    string result = await response.Content.ReadAsStringAsync();
                    LegKeyStatusResponseModel? legKeyStatusResponse = JsonConvert.DeserializeObject<LegKeyStatusResponseModel>(result);
                    if (legKeyStatusResponse != null)
                    {
                        int? _statusCode = legKeyStatusResponse?.Data?.OperationDetails?.Status; 
                        legKeyStatusFlightInformationModel.scheduledDepartureTime = legKeyStatusResponse.Data.OperationDetails?.TripOperationTimes?.DepartureTimes?.Scheduled;
                        legKeyStatusFlightInformationModel.scheduledArrivalTime = legKeyStatusResponse.Data.OperationDetails?.TripOperationTimes?.ScheduledArrivalTime;
                        if (_statusCode == 2)
                        {
                            legKeyStatusFlightInformationModel.status = Enum_DisruptionType.cancelled.ToString();
                        }
                        else if (legKeyStatusResponse?.Data?.OperationDetails?.TripOperationTimes?.TouchDownTimes.Estimated != null)
                        {
                            legKeyStatusFlightInformationModel.EstimateDepartureTime = legKeyStatusResponse.Data.OperationDetails?.TripOperationTimes?.DepartureTimes?.Estimated;
                            legKeyStatusFlightInformationModel.EstimateArrivalTime = legKeyStatusResponse.Data.OperationDetails?.TripOperationTimes?.TouchDownTimes?.Estimated;

                            await _logHelper.LogInfo(JsonConvert.SerializeObject(legKeyStatusFlightInformationModel));
                           
                            StatusUpdate(appParameterTable, legKeyStatusFlightInformationModel);
                        }
                        else
                        {
                            legKeyStatusFlightInformationModel.status = Enum_DisruptionType.NotPostPonedNorCancelled.ToString();
                        }

                    }
                }
                return legKeyStatusFlightInformationModel;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- GetLegKeyStatusAsync"}{" msg:-"} {ex?.Message??""}:- {":- End "}{":- LegKey"}{LegKey??""}");
                throw new InvalidOperationException(ex.Message);
            }
        }

        private static void StatusUpdate(Dictionary<string, string> appParameterTable, LegKeyStatusFlightInformationModel legKeyStatusFlightInformationModel)
        {
            //Estimate a minimum delay of 2 hours and a minimum advancement of 1 hour.
            TimeSpan delayThreshold = TimeSpan.FromMinutes(Convert.ToInt32(appParameterTable[Enum_AppParameters.MinimumDelay.ToString()]));
            TimeSpan advancementThreshold = TimeSpan.FromMinutes(Convert.ToInt32(appParameterTable[Enum_AppParameters.PreponMin.ToString()]));

            //Postpond
            if (legKeyStatusFlightInformationModel.EstimateDepartureTime > legKeyStatusFlightInformationModel.scheduledDepartureTime + delayThreshold)
            {
                legKeyStatusFlightInformationModel.status = Enum_DisruptionType.delayed.ToString();
            }

            //prepond
            else if (legKeyStatusFlightInformationModel.EstimateDepartureTime < legKeyStatusFlightInformationModel.scheduledDepartureTime - advancementThreshold)
            {
                legKeyStatusFlightInformationModel.status = Enum_DisruptionType.advanced.ToString();
            }
            else
            {
                throw new InvalidOperationException("not delayed by at least 2 hours or not prepond by 1 hour.");
            }
        }

        // return the Leg  Details
        public async Task<TripinfoLegListModel?> GetLegKeyManifestDetailsAsync(TripInfoLegsRequest? disruptedFlightRequestModel, string? Token)
        {
            try
            {
                List<string> finalLegKey = new List<string>();
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(disruptedFlightRequestModel);
                HttpResponseMessage response = await HTTPMethod(Token, jsonPayload);

                string responseContent = await response.Content.ReadAsStringAsync();
                TripinfoLegListModel? tripInfoLegsResponseModel = JsonConvert.DeserializeObject<TripinfoLegListModel>(responseContent);
                if (tripInfoLegsResponseModel != null && tripInfoLegsResponseModel.data != null && tripInfoLegsResponseModel.data.Count() > 0 && tripInfoLegsResponseModel.data[0].journeys !=null)
                {
                    foreach (var segment in tripInfoLegsResponseModel.data[0].journeys)
                    {
                        segment.segments?.RemoveAll(segment => segment?.identifier?.identifier != disruptedFlightRequestModel.Identifier);
                    }
                    return tripInfoLegsResponseModel;
                }
                return null;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"GetLegKeyManifestDetailsAsync"}{" msg:-"} {ex?.Message??""}:- {":- End :- "}{JsonConvert.SerializeObject(disruptedFlightRequestModel)}");
                return  null;
            }
        }
        public async Task<ShortPNRModel?> GetLegKeyManifestAsync(TripinfoLegListModel? tripinfoLegListModel)
        {
            try
            {
                ShortPNRModel shortPNRModel = new ShortPNRModel();      
                HashSet<string> setofLegKey = new HashSet<string>();
                if (tripinfoLegListModel != null && tripinfoLegListModel.data != null && tripinfoLegListModel.data.Any() && tripinfoLegListModel.data[0].journeys?.Count > 0)
                {
                    foreach (var journey in tripinfoLegListModel?.data?[0].journeys)
                    {
                        foreach (var segments in journey?.segments)
                        {
                            foreach (var leg in segments.legs)
                            {
                                setofLegKey.Add(leg.legKey);
                            }
                            if (!shortPNRModel.OriginDestination.ContainsKey(segments?.designator?.origin))
                            {
                                shortPNRModel.OriginDestination.Add(segments?.designator?.origin ?? "", segments?.designator?.destination);
                            }
                        }
                    }
                    shortPNRModel?.finalLegKey?.AddRange(setofLegKey);
                }
                else
                {
                    throw new InvalidOperationException("ManifestStatusService --> GetLegKeyManifestAsync()  No get the Trip Info : api/nsk/v2/trip/info");
                }
                return shortPNRModel;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- GetLegKeyManifestAsync"}{" msg:-"} {ex?.Message??""}:- {":- End "}");
                throw new InvalidOperationException(ex.Message);
            }
        }
        //Get the Mainfest Details To find the departure tume
        public async Task<TripinfoLegListModel> GetManifestDetailsAsync(TripInfoLegsRequest disruptedFlightRequestModel, string? Token)
        {
            try
            {
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(disruptedFlightRequestModel);
                HttpResponseMessage response = await HTTPMethod(Token, jsonPayload);
                string responseContent = await response.Content.ReadAsStringAsync();
                TripinfoLegListModel? tripInfoLegsResponseModel = JsonConvert.DeserializeObject<TripinfoLegListModel>(responseContent);
                if (tripInfoLegsResponseModel != null && tripInfoLegsResponseModel.data != null && tripInfoLegsResponseModel.data.Count() > 0)
                {
                    //Is Any function being used? Why Because in VIA flights, both flight numbers are the same.
                    tripInfoLegsResponseModel.data[0].journeys.RemoveAll(Journey => Journey.segments.Any(segment => segment?.identifier?.identifier != disruptedFlightRequestModel.Identifier));

                    return tripInfoLegsResponseModel;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- GetManifestDetailsAsync"}{" msg:-"} {ex?.Message??""}:- {":- End "}");
                throw new InvalidOperationException(ex.Message);
            }
        }
        private async Task<HttpResponseMessage> HTTPMethod(string? Token, string jsonPayload)
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var response = await _httpClient.PostAsync("", content);
            var result = await response.Content.ReadAsStringAsync();
            return response;
        }
    }
}