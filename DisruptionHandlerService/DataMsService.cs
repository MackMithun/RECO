using AutoMapper;
using Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RECO.DistrubtionHandler_MS.Common;
using RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.IUtilities;
using RECO.DistrubtionHandler_MS.Models;
using RECO.DistrubtionHandler_MS.Models.DatabaseModel;
using RECO.DistrubtionHandler_MS.Models.RequestModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel.NHub;
using System.Globalization;
using System.Text;

namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService
{
    public class DataMsService : IDataMsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IBaseService _baseService;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        public DataMsService(HttpClient httpClient, ILogHelper logHelper, IBaseService baseService, 
            IMapper mapper,IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logHelper = logHelper;
            _baseService = baseService;
            _configuration = configuration;
            _mapper = mapper;   
        }
        public async Task<List<DisruptedFlightResponse>> ListOfDisruptedFlight()
        {
            try
            {
                #region newcode  
                string disruptedFlights = await SendRequest("/GetDisruptedFlights");
                if(!string.IsNullOrEmpty(disruptedFlights))
                {
                    return DiscruptedResult<List<DisruptedFlightResponse>>(disruptedFlights) ?? new List<DisruptedFlightResponse>();
                }
                return new List<DisruptedFlightResponse>();
                #endregion
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- ListOfDisruptedFlight"} :- {ex.Message}");
                return new List<DisruptedFlightResponse>();
            }
        }
        public async Task<Dictionary<string, string>> GetAppParameterList()
        {
            try
            {
                #region newcode  
                string response = await SendRequest("/GetAppParameters");
                if (!string.IsNullOrEmpty(response))
                {
                    JObject jsonObject = JObject.Parse(response);
                    JArray dataArray = (JArray)jsonObject["data"];
                    Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();

                    foreach (var item in dataArray)
                    {
                        string parameterName = item["parameterName"].ToString();
                        string parameterValue = item["parameterValue"].ToString();
                        keyValuePairs.Add(parameterName, parameterValue);
                    }

                    return keyValuePairs;
                }
                else
                {
                    throw new InvalidOperationException("Data MS:No Data available AppParametersList");
                }
                #endregion
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- GetAppParameterList"} :- {ex.Message}");
                throw new InvalidOperationException("Data MS: AppParameters API Failed");
            }
        }

        public async Task<ExceptionFlightResponse?> GetExceptionList(NavitaireFlightRequest navitaireRequest)
        {
            try
            {
                ExceptionFlightRequest exceptionFlight = bindExceptionRequest(navitaireRequest);
                var response = await PostJsonAsync<ExceptionFlightRequest>("/FetchExceptionFlightByIdentifier", exceptionFlight);
                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    return RetrieveResult(result);
                }
                else
                {
                    return new ExceptionFlightResponse();
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- GetExceptionList"} :- {ex?.Message??""}");
                throw new InvalidOperationException("Get Exception Table Failed");
            }
        }
        public async Task<bool> PostHistoryDetails(DisruptedFlight disruptedFlight)
        {
            try
            {
                HistoryTable historyTable = _mapper.Map<HistoryTable>(disruptedFlight);
                var result = await PostJsonAsync<HistoryTable>("/SaveDisruptedFlightHistory", historyTable);
                if (result != null)
                {
                    var responseContent = await result.Content.ReadAsStringAsync();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- PostHistoryDetails"} :- {ex.Message}");
                return false;
            }
        }
        public async Task<bool> PostdisruptedFlightDetails(DisruptedFlightsResponse disruptedFlightRequestModel)
        {
            try
            {
                var result = await PostJsonAsync<DisruptedFlightsResponse>("/AddDisruptedFlights", disruptedFlightRequestModel);
                if (result.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    throw new InvalidOperationException("Data MS: Disrupted Flight API Failed. Data not inserted..");
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- PostdisruptedFlightDetails"} :- {ex.Message}");
                return false;
            }
        }
        public async Task<bool> updatedisruptedFlightDetails(UpdateDisruptedFlights updateDisruptedFlightsModel)
        {
            try
            {
                var result = await PostJsonAsync<UpdateDisruptedFlights>("/UpdateDisruptedFlights", updateDisruptedFlightsModel);
                if (result.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    throw new InvalidOperationException("Data MS: update Disrupted Flight API Failed. Data not updated..");
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- updatedisruptedFlightDetails"} :- {ex.Message}");
                return false;
            }
        }
        public async Task<bool> AddCrewMembers(List<CrewMember> crewMembers)
        {
            try
            {
                if (crewMembers != null && crewMembers.Count() > 0)
                {
                    var response = await PostJsonAsync<List<CrewMember>>("/AddBulkCrewMembers", crewMembers);
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    _logHelper.LogInfo($"AddCrewMembers :- END {"Status : "}{response.IsSuccessStatusCode}");
                }
                return false;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- AddCrewMembers"} :- {ex.Message}");
                return false;
            }
        }

        //Temp Database
        public async Task<DisruptedFlightResponse> CheckDisruptedFlightExists(DisruptedFlight disruptedFlight)
        {
            try
            {
                DisruptedFlightRequest disrupted = bindDisruptedRequest(disruptedFlight);
                #region newcode 
                var response =await PostJsonAsync<DisruptedFlightRequest>("/FetchDisruptedFlightByIdentifier", disrupted);
                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    DisruptedFlightResponse disruptedFlightResponse= DiscruptedResult<DisruptedFlightResponse>(result);
                    if(disruptedFlightResponse != null)
                    {
                        return disruptedFlightResponse;
                    }
                    else
                    {
                        return new DisruptedFlightResponse();
                    }
                         
                }
                return new DisruptedFlightResponse();
                #endregion
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- CheckDisruptedFlightExists"} :- {ex.Message}");
                return new DisruptedFlightResponse();
            }
        }
        public async Task<NhbTemplateResponse> GetNhbTemplate()
        {
            try
            {
                Nhub_REQ nhub_REQ = new Nhub_REQ
                {
                    Action="get"
                };
                var response = await PostJsonAsync<Nhub_REQ>("/ManageNHUBtemplates", nhub_REQ);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    NhbTemplateResponse? nhbvariable= JsonConvert.DeserializeObject<NhbTemplateResponse>(responseContent);
                    if(nhbvariable!=null && nhbvariable.data!=null && nhbvariable.data.Count()>0)
                    {
                        return nhbvariable; 
                    }
                }
                _logHelper.LogInfo($"GetNhbTemplate :- END {"Status : "}{response.IsSuccessStatusCode}");
                return new NhbTemplateResponse();
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- GetNhbTemplate"} :- {ex.Message}");
                return new NhbTemplateResponse();
            }
        }
        private async Task<HttpResponseMessage> PostJsonAsync<T>(string endpoint, T data)
        {
            string jsonBody = JsonConvert.SerializeObject(data);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("GatewayKey"));
            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer "+ _configuration.GetValue<string>("InternalAuthorization"));
            return await _httpClient.PostAsync(_httpClient.BaseAddress + endpoint, content);
        }
        private async Task<string> SendRequest(string endpoint)
        {
            var response = await _baseService.SendAsync<BaseResponse>(new BaseRequests()
            {
                ApiType = ApiType.GET,
                Url = _configuration.GetSection("InternalMSAPI")["RecoDataMS"] + endpoint,
                user_key = _configuration.GetValue<string>("GatewayKey")
            }, _configuration.GetValue<string>("InternalAuthorization"));
            if (response.Result != null)
            {
                return response?.Result?.ToString()??"";
            }
            else
            {
                _logHelper.LogInfo($"*******  End {endpoint} ******** Response :" + response.Message);
                return "";
            }
        }
        private ExceptionFlightRequest bindExceptionRequest(NavitaireFlightRequest? navitaireRequest)
        {
            ExceptionFlightRequest exceptionFlight = _mapper.Map<ExceptionFlightRequest>(navitaireRequest);
            exceptionFlight.FlightDate = FormatFlightDateExc(exceptionFlight?.FlightDate ?? "");
            return exceptionFlight;
        }
        private string FormatFlightDateExc(string? flightDate)
        {
            string[] formats = {
                "M/d/yyyy h:mm:ss tt", "M/d/yyyy H:mm:ss", "dd-MM-yyyy HH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ", "MM/dd/yyyy",
                "dd/MM/yyyy", "MM/dd/yyyy HH:mm", "dd/MM/yyyy HH:mm",
                "dddd, dd MMMM yyyy", "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'",
                "yyyy-MM-dd HH:mm:ss", "yyyyMMddTHHmmss", "yyyy-MM-ddTHH:mm:ss.fff",
                "yyyy-MM-ddTHH:mm:ss.fffZ"
            };
            DateTime parsedDate;
            if (DateTime.TryParseExact(flightDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
            {
                return parsedDate.ToString("yyyy-MM-dd");
            }
            return flightDate;
        }
        private string FormatFlightDate(string? flightDate)
        {
            string[] formats = {
                "M/d/yyyy h:mm:ss tt", "M/d/yyyy H:mm:ss", "dd-MM-yyyy HH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ", "MM/dd/yyyy",
                "dd/MM/yyyy", "MM/dd/yyyy HH:mm", "dd/MM/yyyy HH:mm",
                "dddd, dd MMMM yyyy", "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'",
                "yyyy-MM-dd HH:mm:ss", "yyyyMMddTHHmmss", "yyyy-MM-ddTHH:mm:ss.fff",
                "yyyy-MM-ddTHH:mm:ss.fffZ"
            };
            DateTime parsedDate;
            if (DateTime.TryParseExact(flightDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
            {
                return parsedDate.ToString("yyyy-MM-ddTHH:mm:ss");
            }
            return flightDate;
        }

        private DisruptedFlightRequest bindDisruptedRequest(DisruptedFlight? disruptedFlight)
        {
            DisruptedFlightRequest disruptedFlightRequest = _mapper.Map<DisruptedFlightRequest>(disruptedFlight);
            disruptedFlightRequest.Date = FormatFlightDate(disruptedFlightRequest?.Date ?? "");
            return disruptedFlightRequest;
        }
        private T DiscruptedResult<T>(string result)
        {
            JToken jsonObject = JObject.Parse(result);
            bool isSuccess = (bool)jsonObject["isSuccess"];
            if (isSuccess)
            {
                if (jsonObject is JObject)
                {
                    var dataObject = jsonObject["data"];
                    if (dataObject != null)
                    {
                        return JsonConvert.DeserializeObject<T>(dataObject.ToString());
                    }
                }
                else if (jsonObject is JArray)
                {
                    var dataArray = (JArray)jsonObject;
                    if (dataArray.Any())
                    {
                        var dataobject = dataArray[0]["data"];
                        if (dataobject != null)
                        {
                            return JsonConvert.DeserializeObject<T>(dataobject.ToString());
                        }
                    }
                }
            }
            return default(T);
        }
        private ExceptionFlightResponse? RetrieveResult(string result)
        {
            JToken jsonObject = JObject.Parse(result);
            if (jsonObject is JObject)
            {
                var dataObject = jsonObject["data"];
                if (dataObject != null)
                {
                    return JsonConvert.DeserializeObject<ExceptionFlightResponse>(dataObject.ToString());
                }
            }
            else if (jsonObject is JArray)
            {
                var dataArray = (JArray)jsonObject;
                if (dataArray.Count > 0)
                {
                    var dataobject = dataArray[0]["data"];
                    if (dataobject != null)
                    {
                        return JsonConvert.DeserializeObject<ExceptionFlightResponse>(dataobject.ToString());
                    }
                }
            }
            return new ExceptionFlightResponse();
        }
    }
}
