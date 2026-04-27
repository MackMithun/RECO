using Newtonsoft.Json;
using RECO.Reaccommodation_MS.Common;
using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.DatabaseModel;
using System.Text;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.ManifestModel;
using Newtonsoft.Json.Linq;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;
using RECO.Reaccommodation_MS.Models.Enum;
using System.Globalization;
using RECO.Reaccommodation_MS.Models.Database;
using AutoMapper;
using RECO.Reaccommodation_MS.Models.ResponseModel.MCT;
using RECO.Reaccommodation_MS.Models.ResponseModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class DataMsService : IDataMsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IBaseService _baseService;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly string _caller = typeof(DataMsService).Name;
        public DataMsService(HttpClient httpClient, ILogHelper logHelper, IMapper mapper, IBaseService baseService, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logHelper = logHelper;
            _baseService = baseService;
            _configuration = configuration;
            _mapper = mapper;       
        }
        public async Task<List<Rule_PNR_PriorityTable>> GetRulePNRPriorityList()
        {
            try
            {
                string GetRulePNRPriority = await SendRequest("/GetRulePNRPriority");
                if (GetRulePNRPriority != null)
                {
                    return DiscruptedResultPNR(GetRulePNRPriority);
                }
                else
                {
                    await _logHelper.LogInfo($"{_caller}:{"GetRulePNRPriorityList :-"} :- {"End Result :-"}{GetRulePNRPriority??""}");
                    return new List<Rule_PNR_PriorityTable>();
                }

            }
            catch (Exception ex)
            {
                await _logHelper.LogError(ex.Message);
                throw new InvalidOperationException(ex.Message);
            }
        }
        public async Task<Dictionary<string, string>> GetAppParameterList()
        {
            try
            {
                #region newcode 
                string GetAppParameters = await SendRequest("/GetAppParameters");
                if (GetAppParameters != null)
                {
                    JObject jsonObject = JObject.Parse(GetAppParameters.ToString());
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
                    await _logHelper.LogInfo($"{_caller}:{"GetAppParameterList : - "}{GetAppParameters??""}");
                    throw new InvalidOperationException("Data MS: Do not retrieve the data into the app parameter table.");
                }
                #endregion
            }
            catch (Exception ex)
            {
                await _logHelper.LogError(ex.Message);
                throw new InvalidOperationException(ex.Message);
            }
        }
        public async Task<List<string>> GetExceptionList(DisruptedFlightRequest disruptedFlightRequest, Dictionary<string, string?>? OriginDestination)
        {
            List<string> FlightNumber = new List<string>();
            try
            {
                foreach (KeyValuePair<string, string?> entry in OriginDestination)
                {
                    disruptedFlightRequest.origin = entry.Key;
                    disruptedFlightRequest.destination= entry.Value;
                    ExceptionFlightRequest exceptionFlightRequest = _mapper.Map<ExceptionFlightRequest>(disruptedFlightRequest);
                    var response = await PostJsonAsync<ExceptionFlightRequest>("/FetchExceptionDetailsByDesignator", exceptionFlightRequest);
                    if (response.IsSuccessStatusCode)
                    {
                        string result = response.Content.ReadAsStringAsync().Result;
                        FlightNumber.AddRange(RetrieveResult(result, disruptedFlightRequest));
                    }
                    else
                    {
                        await _logHelper.LogInfo($"{_caller}:{"GetExceptionList"} :- {" :- End response :-"}{response.IsSuccessStatusCode}");
                        FlightNumber.AddRange(new List<string>());
                    }
                }
                return FlightNumber;    
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{"GetExceptionList : -"}:-{ex.Message}");
                await _logHelper.LogConsoleException(ex);
                return FlightNumber;
            }
        }
        public async Task<DisruptedFlightDB> GetDisruptedFlight(DisruptedFlight disruptedFlight)
        {
            try
            {
                DisruptedFlightRequestDB disrupted = bindDisruptedRequest(disruptedFlight);
                #region newcode 
                var response = await PostJsonAsync<DisruptedFlightRequestDB>("/FetchDisruptedFlightByIdentifier", disrupted);
                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    return DiscruptedResult(result);
                }
                return new DisruptedFlightDB();
                #endregion
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{" :- GetDisruptedFlight"} :- {ex.Message}");
                return new DisruptedFlightDB();
            }
        }
        public async Task<bool> AddPNRDetails(List<SortedPNR> sortedPNR)
        {
            try
            {
                var modifiedListOfPNR = sortedPNR.Select(pnr => new SortedPNR
                {
                    FLTID = pnr.FLTID,
                    PNRCode = pnr.PNRCode,
                    EmailId = pnr.EmailId,  
                    ContactNo = pnr.ContactNo,  
                    RECOStatus = pnr.RECOStatus,
                    ReasonForFailure = pnr.ReasonForFailure,
                    Priority = pnr.Priority,
                    CreatedBy= pnr.CreatedBy,     
                }).ToList();
                var result = await PostJsonAsync<List<SortedPNR>>("/AddBulkPNRDetails", modifiedListOfPNR);
                if (result.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    await _logHelper.LogInfo($"{_caller}:{" :- AddPNRDetails"} :- {"end"}{" :-PNR Details:- "}");
                    await _logHelper.LogInfo($"{_caller}:{" :- AddPNRDetails"} :- {"end"}{" :-Status:- "}{result.IsSuccessStatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{" :- AddPNRDetails"} :- {ex.Message}");
                await _logHelper.LogError(ex.Message);
                return false;
            }
        }
        //Temp Database 
        public async Task<List<PNRDetail>> GetPNRDetails(DisruptedFlightDB? disruptedFlightDB)
        {
            try
            {
                #region newcode 
                string PnrDetail = await SendRequest("/GetPnrDetailById?id=" + disruptedFlightDB.FLTID);
                if (PnrDetail != null)
                {
                    JToken jsonObject = JObject.Parse(PnrDetail);
                    if (jsonObject is JObject)
                    {
                        var dataObject = jsonObject["data"];
                        if (dataObject != null)
                        {
                            List<PNRTableModel> pNRTableModels = JsonConvert.DeserializeObject<List<PNRTableModel>>(dataObject.ToString());
                            pNRTableModels.Where(x => x.PNRRECOStatus != Enum_PNR.Success.ToString()).ToList();
                            List<PNRDetail> updatePNRTableModels = pNRTableModels
                                 .Select(x => new PNRDetail
                                 {
                                     PNRID = x.PNRID,
                                     FLTID = x.FLTID,
                                     PNRCode = x.PNRCode,
                                     EmailId=x.EmailId,
                                     ContactNo=x.ContactNo,
                                     RECOStatus = x.PNRRECOStatus,
                                     ReasonForFailure = x.ReasonForFailure,
                                     Priority = x.Priority,
                                     CreatedBy=x.CreatedBy, 
                                     ModifiedBy = x.ModifiedBy, 
                                 }).ToList();
                            return updatePNRTableModels;
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
                                List<PNRTableModel> pNRTableModels = JsonConvert.DeserializeObject<List<PNRTableModel>>(dataobject.ToString());
                                pNRTableModels.Where(x => x.PNRRECOStatus != Enum_PNR.Success.ToString()).ToList();
                                List<PNRDetail> updatePNRTableModels = pNRTableModels
                                     .Select(x => new PNRDetail
                                     {
                                         PNRID = x.PNRID,
                                         FLTID = x.FLTID,
                                         PNRCode = x.PNRCode,
                                         EmailId=x.EmailId,
                                         ContactNo = x.ContactNo,   
                                         RECOStatus = x.PNRRECOStatus,
                                         ReasonForFailure = x.ReasonForFailure,
                                         Priority = x.Priority,
                                         CreatedBy = x.CreatedBy,
                                         ModifiedBy = x.ModifiedBy, 
                                     }).ToList();
                                return updatePNRTableModels;
                            }
                        }
                    }
                }
                return new List<PNRDetail>();
                #endregion
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{" :- GetPNRDetails"} :- {ex.Message}");
                await _logHelper.LogError(ex.Message);
                return new List<PNRDetail>(); ;
            }
        }
        // Temp Dataabase
        public async Task<string> UpdatePNRDetails(List<PNRDetail> pnrDetail)
        {
            try
            {
                var result = await PutJsonAsync<List<PNRDetail>>("/UpdateBulkPNRDetails", pnrDetail);
                if (result.IsSuccessStatusCode)
                {
                    return "true";
                }
                else
                {
                    await _logHelper.LogInfo($"{_caller}:{" :- UpdatePNRDetails"} :- {"end"}{" :-PNR Details:- "}");
                    await _logHelper.LogInfo($"{_caller}:{" :- UpdatePNRDetails"} :- {"end Success:- "}{result.IsSuccessStatusCode}");
                    return "false";
                }

            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{":- UpdatePNRDetails"}:- {ex.Message}");
                return "false";
            }

        }
        // Temp Dataabase
        public async Task<string> UpdateDisruptedflights(UpdateDisruptedFlights updateDisruptedFlights)
        {
            try
            {
                var result = await PostJsonAsync<UpdateDisruptedFlights>("/UpdateDisruptedFlights", updateDisruptedFlights);
                if (result.IsSuccessStatusCode)
                {
                    return "true";
                }
                else
                {
                    await _logHelper.LogInfo($"{_caller}:{" :- UpdateDisruptedflights"} :- {"end"}{" :-Disrupted flights Details:- "}{JsonConvert.SerializeObject(updateDisruptedFlights)}");
                    await _logHelper.LogInfo($"{_caller}:{" :- UpdateDisruptedflights"} :- {"End Success:- "}{result.IsSuccessStatusCode}");
                    return "false";
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{" :- UpdateDisruptedflights"}:- {ex.Message}");
                return "false";
            }
        }
        //Temp Database
        public async Task<string> PostRoutingDetails(List<RoutingDetail> routingDetail)
        {
            try
            {
                var result = await PostJsonAsync<List<RoutingDetail>>("/AddBulkRouting", routingDetail);
                if (result.IsSuccessStatusCode)
                {
                    return "true";
                }
                else
                {
                    await _logHelper.LogInfo($"{_caller}:{" :- PostRoutingDetails"} :- {"end"}{" :-Routing Details:- "}{JsonConvert.SerializeObject(routingDetail)}");
                    await _logHelper.LogInfo($"{_caller}:{" :- PostRoutingDetails"} :- {" Success:- "}{result.IsSuccessStatusCode}");
                    return "false";
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{":- PostRoutingDetails"}:-{ex.Message}");
                return "false";
            }
        }
        //MCT
        public async Task<double> CheckMCTConnectionTime(string StationCode, string flightType)
        {
            try
            {
                var Request = new MctStationRequest
                {
                    StationCode = StationCode,
                    ConnectionType = flightType,
                };
                var responseMessage = await PostJsonAsync<MctStationRequest>("/FetchMCTRulesDetails", Request);
                if (responseMessage.IsSuccessStatusCode)
                {
                    string result = responseMessage.Content.ReadAsStringAsync().Result;
                    if (!string.IsNullOrEmpty(result))
                    {
                        List<MCT_StationConnectionRule>? multi_StationConnections = JsonConvert.DeserializeObject<List<MCT_StationConnectionRule>>(result);
                        MCT_StationConnectionRule? _stationConnectionRuleModel_SDT = multi_StationConnections?.OrderBy(x => x.MinCnxTime).FirstOrDefault();
                        return _stationConnectionRuleModel_SDT?.MinCnxTime ?? 60;
                    }
                }

                await _logHelper.LogInfo($"{_caller}:{" :- CheckMCTConnectionTime:- "} :- {"End:- "}{responseMessage.IsSuccessStatusCode}:- {StationCode} -: {flightType}");
                return 60;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{" :- CheckMCTConnectionTime"} :- {"End msg :" + ex.Message}");
                await _logHelper.LogConsoleException(ex);
                return 60;
            }
        }
        public async Task<MCT_ImpactedFlight> GetStationConnectionRuleList(MCT_ImpactedFlight impactedFlightModel)
        {
            try
            {
                HttpClient _httpClient = new HttpClient();
                if (impactedFlightModel.mctApplicable)
                {

                    if (impactedFlightModel.ArrivalStationCode != null && impactedFlightModel.DepartureStationCode != null)
                    {
                        async Task<string> GetMCTRulesDetails(MctStationRequest request)
                        {
                            var response = await PostJsonAsync<MctStationRequest>("/FetchMCTRulesDetails", request);
                            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;

                        }
                        var SDTRequest = new MctStationRequest
                        {
                            StationCode = impactedFlightModel.DepartureStationCode,
                            ConnectionType = impactedFlightModel.flightType.ToString(),
                        };
                        var SATRequest = new MctStationRequest
                        {
                            StationCode = impactedFlightModel.ArrivalStationCode,
                            ConnectionType = impactedFlightModel.flightType.ToString(),
                        };

                        var resultSDT = await GetMCTRulesDetails(SDTRequest);
                        var resultSAT = await GetMCTRulesDetails(SATRequest);

                        return GetNearestJourneySAT_SDT(impactedFlightModel, resultSDT, resultSAT);
                    }
                    else if (impactedFlightModel.ArrivalStationCode != null)
                    {
                        var SATRequest = new MctStationRequest
                        {
                            StationCode = impactedFlightModel.ArrivalStationCode,
                            ConnectionType = impactedFlightModel.flightType.ToString(),
                        };
                        var response = await PostJsonAsync<MctStationRequest>("/FetchMCTRulesDetails", SATRequest);
                        if (response.IsSuccessStatusCode)
                        {
                            string resultSAT = response.Content.ReadAsStringAsync().Result;
                            return GetNearestJourneySAT_SDT_ArrivalStationCode(impactedFlightModel, resultSAT);
                        }
                        else
                        {
                            impactedFlightModel.NearestJourneySAT = ((DateTime)impactedFlightModel?.NearestJourneySAT).AddMinutes(60);
                            impactedFlightModel.NearestJourneySDT = DateTime.Now.AddDays(14);
                            return impactedFlightModel;
                        }
                    }
                    else if (impactedFlightModel.DepartureStationCode != null)
                    {
                        var SDTRequest = new MctStationRequest
                        {
                            StationCode = impactedFlightModel.DepartureStationCode,
                            ConnectionType = impactedFlightModel.flightType.ToString(),
                        };
                        var responseMessage = await PostJsonAsync<MctStationRequest>("/FetchMCTRulesDetails", SDTRequest); //GetMCTRulesDetails
                        if (responseMessage.IsSuccessStatusCode)
                        {
                            string resultSDT = responseMessage.Content.ReadAsStringAsync().Result;
                            if (!string.IsNullOrEmpty(resultSDT))
                            {

                                List<MCT_StationConnectionRule> stationConnectionRuleModel_SDT = JsonConvert.DeserializeObject<List<MCT_StationConnectionRule>>(resultSDT);
                                MCT_StationConnectionRule _stationConnectionRuleModel_SDT = stationConnectionRuleModel_SDT.OrderBy(x => x.MinCnxTime).FirstOrDefault();
                                impactedFlightModel.NearestJourneySAT = DateTime.Now.AddDays(-4);
                                impactedFlightModel.NearestJourneySDT = ((DateTime)impactedFlightModel?.NearestJourneySDT).AddMinutes(-_stationConnectionRuleModel_SDT.MinCnxTime);
                                return impactedFlightModel;
                            }
                            else
                            {
                                impactedFlightModel.NearestJourneySAT = DateTime.Now.AddDays(-4);
                                impactedFlightModel.NearestJourneySDT = ((DateTime)impactedFlightModel?.NearestJourneySDT).AddMinutes(-60);
                                return impactedFlightModel;
                            }

                        }
                        else
                        {
                            await _logHelper.LogError($"{_caller}:{" :- GetStationConnectionRuleList"} :- {"End "}{":- Status:-"}{responseMessage.IsSuccessStatusCode}");
                            impactedFlightModel.NearestJourneySAT = DateTime.Now.AddDays(-4);
                            impactedFlightModel.NearestJourneySDT = ((DateTime)impactedFlightModel?.NearestJourneySDT).AddMinutes(-60);
                            return impactedFlightModel;
                        }
                    }
                }
                else
                {
                    impactedFlightModel.NearestJourneySAT = DateTime.Now.AddDays(-4);
                    impactedFlightModel.NearestJourneySDT = DateTime.Now.AddDays(14);
                }
                return impactedFlightModel;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{":- GetStationConnectionRuleList"} :- {"End msg :" + ex.Message}");
                await _logHelper.LogConsoleException(ex);
                return impactedFlightModel;
            }
        }
        public async Task<NhbTemplateResponse> GetNhbTemplate()
        {
            try
            {
                Nhub_REQ nhub_REQ = new Nhub_REQ
                {
                    Action = "get"
                };
                var response = await PostJsonAsync<Nhub_REQ>("/ManageNHUBtemplates", nhub_REQ);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    NhbTemplateResponse? nhbvariable = JsonConvert.DeserializeObject<NhbTemplateResponse>(responseContent);
                    if (nhbvariable != null && nhbvariable.data?.Count() > 0)
                    {
                        return nhbvariable;
                    }
                }
                await _logHelper.LogInfo($"GetNhbTemplate :- END {"Status : "}{response.IsSuccessStatusCode}");
                return new NhbTemplateResponse();
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{" :- GetNhbTemplate"} :- {ex.Message}");
                return new NhbTemplateResponse();
            }
        }
        public async Task<FetchDashboardResponse> fetchDashboardDetails(FetchDashboardRequest dashboardDetails)
        {
            try
            {
                var response = await PostJsonAsync<FetchDashboardRequest>("/FetchDashboardDetails", dashboardDetails);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    FetchDashboard? fetchDashboard= JsonConvert.DeserializeObject<FetchDashboard>(responseContent);
                    if (fetchDashboard != null && fetchDashboard.data!= null)
                    {
                        return fetchDashboard.data;
                    }
                }
                return new FetchDashboardResponse();
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{" :- fetchDashboardDetails"} :- {ex.Message} -:- {JsonConvert.SerializeObject(dashboardDetails)}");
                return new FetchDashboardResponse();
            }
        }
        private async Task<HttpResponseMessage> PostJsonAsync<T>(string endpoint, T data)
        {
            string jsonBody = JsonConvert.SerializeObject(data);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("GatewayKey"));
            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _configuration.GetValue<string>("InternalAuthorization"));
            return await _httpClient.PostAsync(_httpClient.BaseAddress + endpoint, content);
        }
        private async Task<HttpResponseMessage> PutJsonAsync<T>(string endpoint, T data)
        {
            string jsonBody = JsonConvert.SerializeObject(data);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("GatewayKey"));
            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _configuration.GetValue<string>("InternalAuthorization"));
            return await _httpClient.PutAsync(_httpClient.BaseAddress + endpoint, content);
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
                return response?.Result.ToString()??"";
            }
            else
            {
               await _logHelper.LogInfo($"*******  End {endpoint} ******** Response :" + response.Message);
                return "";
            }
        }
        private DisruptedFlightRequestDB bindDisruptedRequest(DisruptedFlight? disruptedFlight)
        {
            DisruptedFlightRequestDB disruptedFlightRequest = _mapper.Map<DisruptedFlightRequestDB>(disruptedFlight);
            string[] formats = {
                "M/d/yyyy h:mm:ss tt", "M/d/yyyy H:mm:ss", "dd-MM-yyyy HH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ", "MM/dd/yyyy",
                "dd/MM/yyyy", "MM/dd/yyyy HH:mm", "dd/MM/yyyy HH:mm",
                "dddd, dd MMMM yyyy", "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'",
                "yyyy-MM-dd HH:mm:ss", "yyyyMMddTHHmmss", "yyyy-MM-ddTHH:mm:ss.fff",
                "yyyy-MM-ddTHH:mm:ss.fffZ"
            };
            DateTime STD1;
            if (DateTime.TryParseExact(disruptedFlightRequest.Date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out STD1))
            {
                DateTime STD = DateTime.ParseExact(disruptedFlightRequest.Date, formats, CultureInfo.InvariantCulture);
                disruptedFlightRequest.Date = STD.ToString("yyyy-MM-ddTHH:mm:ss");
            }
            return disruptedFlightRequest;
        }
        private static MCT_ImpactedFlight GetNearestJourneySAT_SDT_ArrivalStationCode(MCT_ImpactedFlight impactedFlightModel, string resultSAT)
        {
            if (!string.IsNullOrEmpty(resultSAT))
            {

                List<MCT_StationConnectionRule> stationConnectionRuleModel_SAT = JsonConvert.DeserializeObject<List<MCT_StationConnectionRule>>(resultSAT);
                MCT_StationConnectionRule _stationConnectionRuleModel_SAT = stationConnectionRuleModel_SAT.OrderBy(x => x.MinCnxTime).FirstOrDefault();
                impactedFlightModel.NearestJourneySAT = ((DateTime)impactedFlightModel?.NearestJourneySAT).AddMinutes(_stationConnectionRuleModel_SAT.MinCnxTime);
                impactedFlightModel.NearestJourneySDT = DateTime.Now.AddDays(14);
                return impactedFlightModel;
            }
            else
            {
                impactedFlightModel.NearestJourneySAT = ((DateTime)impactedFlightModel?.NearestJourneySAT).AddMinutes(60);
                impactedFlightModel.NearestJourneySDT = DateTime.Now.AddDays(14);
                return impactedFlightModel;
            }
        }
        private static MCT_ImpactedFlight GetNearestJourneySAT_SDT(MCT_ImpactedFlight impactedFlightModel, string resultSDT, string resultSAT)
        {
            if (!string.IsNullOrEmpty(resultSDT) && !string.IsNullOrEmpty(resultSAT))
            {

                List<MCT_StationConnectionRule> stationConnectionRuleModel_SDT = JsonConvert.DeserializeObject<List<MCT_StationConnectionRule>>(resultSDT);
                MCT_StationConnectionRule _stationConnectionRuleModel_SDT = stationConnectionRuleModel_SDT.OrderBy(x => x.MinCnxTime).FirstOrDefault();
                List<MCT_StationConnectionRule> stationConnectionRuleModel_SAT = JsonConvert.DeserializeObject<List<MCT_StationConnectionRule>>(resultSAT);
                MCT_StationConnectionRule _stationConnectionRuleModel_SAT = stationConnectionRuleModel_SAT.OrderBy(x => x.MinCnxTime).FirstOrDefault();
                impactedFlightModel.NearestJourneySAT = ((DateTime)impactedFlightModel?.NearestJourneySAT).AddMinutes(_stationConnectionRuleModel_SAT.MinCnxTime);
                impactedFlightModel.NearestJourneySDT = ((DateTime)impactedFlightModel?.NearestJourneySDT).AddMinutes(-_stationConnectionRuleModel_SDT.MinCnxTime);
                return impactedFlightModel;
            }
            else
            {
                impactedFlightModel.NearestJourneySAT = ((DateTime)impactedFlightModel?.NearestJourneySAT).AddMinutes(60);
                impactedFlightModel.NearestJourneySDT = ((DateTime)impactedFlightModel?.NearestJourneySDT).AddMinutes(-60);
                return impactedFlightModel;
            }
        }
        public async Task<MCT_ImpactedFlight> StationConnectionTimeList(MCT_ImpactedFlight impactedFlightModel)
        {
            try
            {
                return impactedFlightModel;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{":- StationConnectionTimeList"} :- {"End msg :" + ex.Message}");
                await _logHelper.LogConsoleException(ex);
                return impactedFlightModel;
            }
        }
        
        private static List<ExceptionFlightListsModel> MapJarray(List<ExceptionFlightListsModel> exceptionFlightListsModel, JToken jsonObject)
        {
            var dataArray = (JArray)jsonObject;
            if (dataArray.Count > 0)
            {
                var dataobject = dataArray[0]["data"];
                if (dataobject != null)
                {
                    exceptionFlightListsModel = JsonConvert.DeserializeObject<List<ExceptionFlightListsModel>>(dataobject.ToString());
                }
            }

            return exceptionFlightListsModel;
        }
        
        private DisruptedFlightDB DiscruptedResult(string result)
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
                        return JsonConvert.DeserializeObject<DisruptedFlightDB>(dataObject.ToString());
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
                            return JsonConvert.DeserializeObject<DisruptedFlightDB>(dataobject.ToString());
                        }
                    }
                }
            }
            return new DisruptedFlightDB();
        }
        private List<Rule_PNR_PriorityTable> DiscruptedResultPNR(string result)
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
                        return JsonConvert.DeserializeObject<List<Rule_PNR_PriorityTable>>(dataObject.ToString());
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
                            return JsonConvert.DeserializeObject<List<Rule_PNR_PriorityTable>>(dataobject.ToString());
                        }
                    }
                }
            }
            return new List<Rule_PNR_PriorityTable>();
        }
        private List<string> RetrieveResult(string result, DisruptedFlightRequest disruptedFlightRequest)
        {
            List<ExceptionFlightListsModel> exceptionFlightListsModel = new List<ExceptionFlightListsModel>();
            //NoMoveOnTo
            if (result != null && result != "")
            {
                JToken jsonObject = JObject.Parse(result);
                if (jsonObject is JObject)
                {
                    var dataObject = jsonObject["data"];
                    if (dataObject != null)
                    {
                        exceptionFlightListsModel = JsonConvert.DeserializeObject<List<ExceptionFlightListsModel>>(dataObject.ToString());
                    }
                }
                else if (jsonObject is JArray)
                {
                    exceptionFlightListsModel = MapJarray(exceptionFlightListsModel, jsonObject);
                }

                return exceptionFlightListsModel.Where(x => x.ExceptionType.ToLower().Trim() == "nomoveonto"
    && ((x.FromDate ?? DateTime.MinValue).ToString("MM-dd-yyyy") == (disruptedFlightRequest.beginDate ?? DateTime.MinValue).ToString("MM-dd-yyyy")))
    .Select(x => x.FlightNumber).ToList();

            }
            else
            {
                _logHelper.LogError($"{_caller}:{"GetExceptionList"}:-{"Data MS: Do not retrieve success responses for the GetExceptionDetailsByDesignator call."}");
                throw new InvalidOperationException("Data MS: Do not retrieve success responses for the GetExceptionDetailsByDesignator call.");
            }
        }
    }
}
