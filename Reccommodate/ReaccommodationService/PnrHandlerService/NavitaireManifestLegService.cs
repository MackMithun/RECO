using Newtonsoft.Json;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.DatabaseModel;
using RECO.Reaccommodation_MS.Models.Enum;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.ManifestModel;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;
using System.Collections.Concurrent;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class NavitaireManifestLegService : INavitaireManifestLegDetailsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        private readonly IPNRPriorityService _pNRPriorityService;    
        public NavitaireManifestLegService(HttpClient httpClient, ILogHelper logHelper, IConfiguration configuration, IPNRPriorityService pNRPriorityService)
        {
            _httpClient = httpClient;
            _logHelper = logHelper;
            _configuration = configuration;
            _pNRPriorityService = pNRPriorityService;       
        }
        public async Task<List<SortedPNR>> GetListShortedPNRAsync(DisruptedFlightDB disruptedFlightDB, IBookingService bookingService, IDataMsService _dataMsService, string LegKey, string Token)
        {
            try
            {
                List<SortedPNR> sortedPNRList = new List<SortedPNR>();
                List<string> _unSortedPNR = new List<string>();
                _unSortedPNR.AddRange(await GetDistinctPNR(Token, LegKey));
                sortedPNRList = await GetShortPNRPriorityBase(disruptedFlightDB, bookingService, _dataMsService, _unSortedPNR,0, Token);
                var remainingPNRs = _unSortedPNR.Except(sortedPNRList.Select(pnr => pnr.PNRCode)).ToList();

                if (remainingPNRs.Count > 0)
                {
                    var additionalSortedPNRs = await GetShortPNRPriorityBase(disruptedFlightDB, bookingService, _dataMsService, remainingPNRs, 1,Token);
                    sortedPNRList.AddRange(additionalSortedPNRs);
                }
                return sortedPNRList;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"GetListShortedPNRAsync"}{" msg:-"} {ex.Message}:- {":- End "}");
                await _logHelper.LogConsoleException(ex);
                throw new InvalidOperationException(ex.Message);
            }

        }
        public async Task<List<SortedPNR>> GetListUnShortedPNRAsync(DisruptedFlightDB disruptedFlightDB, IBookingService _bookingService, string LegKey, string Token)
        {
            try
            {
                List<SortedPNR> sortedPNR = new List<SortedPNR>();
                sortedPNR = await GetDistinctUnshortedPNR(disruptedFlightDB, _bookingService,LegKey, Token);
                return sortedPNR;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"GetListUnShortedPNRAsync"}{" msg:-"} {ex.Message}:- {":- End "}");
                await _logHelper.LogConsoleException(ex);
                throw new InvalidOperationException(ex.Message);
            }

        }
        private async Task<List<SortedPNR>> GetShortPNRPriorityBase(DisruptedFlightDB disruptedFlightDB, IBookingService bookingService, IDataMsService _dataMsService, List<string> _TemplistOfPNR,int Flag, string Token)
        {
            try
            {
                List<SortedPNR> listOFPNRDetailsModels = new List<SortedPNR>();
                listOFPNRDetailsModels = await databasePriorityAndFliterOut(disruptedFlightDB, bookingService, _dataMsService, _TemplistOfPNR, Flag, Token);
                return listOFPNRDetailsModels.OrderBy(x => x.Priority).ToList();
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"GetShortPNRPriorityBase"}{" msg:-"} {ex.Message}:- {":- End "}");
                await _logHelper.LogConsoleException(ex);
                throw new InvalidOperationException(ex.Message);
            }
        }
        private async Task<List<SortedPNR>> databasePriorityAndFliterOut(DisruptedFlightDB disruptedFlightDB, IBookingService _bookingService, IDataMsService _dataMsService, List<string> _TemplistOfPNR,int  Flag, string Token)
        {
            try
            {
                List<SortedPNR> FindThePNR_Priority = new List<SortedPNR>();
                if (Flag==1)
                {
                    foreach (var PNR in _TemplistOfPNR)
                    {
                        try
                        {
                            await _logHelper.LogInfo(" :- Sorted PNRs :- " + PNR+" :- Flag:- "+ Flag);
                            var bookingDetails = await _bookingService.GetBookingByRecordLocator(PNR, Token);
                            if (bookingDetails != null && bookingDetails != "")
                            {
                                BookingDetails? booking = JsonConvert.DeserializeObject<BookingDetails>(bookingDetails);
                                if (booking != null && booking.Data != null && booking.Data.Journeys.Count() > 0)
                                {
                                    string PhoneNumbers = string.Join(", ", new[]
                                    {
                                   string.Join(", ", booking?.Data?.Contacts?.P?.PhoneNumbers?.Take(2).Select(p => p.Number) ?? new List<string>()),
                                   string.Join(", ", booking?.Data?.Contacts?.H?.PhoneNumbers?.Take(2).Select(p => p.Number) ?? new List<string>()),
                                   string.Join(", ", booking?.Data?.Contacts?.D?.PhoneNumbers?.Take(2).Select(p => p.Number) ?? new List<string>()),
                                   string.Join(", ", booking?.Data?.Contacts?.I?.PhoneNumbers?.Take(2).Select(p => p.Number) ?? new List<string>()),
                                   string.Join(", ", booking?.Data?.Contacts?.O?.PhoneNumbers?.Take(2).Select(p => p.Number) ?? new List<string>()),
                                   string.Join(", ", booking?.Data?.Contacts?.W?.PhoneNumbers?.Take(2).Select(p => p.Number) ?? new List<string>())
                               }.Where(s => !string.IsNullOrEmpty(s)));

                                    string EmailAddress = string.Join(", ", new[]
                                    {
                                   booking?.Data?.Contacts?.P?.EmailAddress ?? "",
                                   booking?.Data?.Contacts?.H?.EmailAddress ?? "",
                                   booking?.Data?.Contacts?.D?.EmailAddress ?? "",
                                   booking?.Data?.Contacts?.I?.EmailAddress ?? "",
                                   booking?.Data?.Contacts?.O?.EmailAddress ?? "",
                                   booking?.Data?.Contacts?.W?.EmailAddress ?? ""
                               }.Where(s => !string.IsNullOrEmpty(s)));


                                    FindThePNR_Priority.Add(new SortedPNR
                                    {
                                        FLTID = disruptedFlightDB.FLTID,
                                        PNRCode = PNR,
                                        EmailId = AesEncryption.AesEncrypt(EmailAddress),
                                        ContactNo = AesEncryption.AesEncrypt(PhoneNumbers),
                                        RECOStatus = Enum_PNR.Failed.ToString(),
                                        ReasonForFailure = "",
                                        Priority = 12,
                                        FlightType = booking.Data.Journeys[0].FlightType,
                                        CreatedBy = disruptedFlightDB.CreatedBy,
                                    });
                                }
                            }

                        }
                        catch(Exception ex)
                        {
                            FindThePNR_Priority.Add(new SortedPNR
                            {
                                FLTID = disruptedFlightDB.FLTID,
                                PNRCode = PNR,
                                EmailId = "",
                                ContactNo = "",
                                RECOStatus = Enum_PNR.Failed.ToString(),
                                ReasonForFailure = "",
                                Priority = 12,
                                FlightType = 999,
                                CreatedBy = disruptedFlightDB.CreatedBy,
                            });
                            await _logHelper.LogError(PNR+" : "+ ex.Message);
                        }
                        
                    }
                }
                else
                {
                    List<Rule_PNR_PriorityTable> rule_PNR_PriorityTables = await _dataMsService.GetRulePNRPriorityList();
                    var tasks = _TemplistOfPNR.Select(async PNR =>
                    {
                        await _logHelper.LogInfo(" :- Sorted PNRs :- " + PNR + " :- Flag:- " + Flag);
                        var bookingDetails = await _bookingService.GetBookingByRecordLocator(PNR, Token);
                        if (bookingDetails != null && bookingDetails != "")
                        {
                            BookingDetails? booking = JsonConvert.DeserializeObject<BookingDetails>(bookingDetails);
                            if (booking != null && booking.Data != null && booking?.Data?.Journeys?.Count() > 0)
                            {
                                int FlightType = booking.Data.Journeys[0].FlightType;
                                await _pNRPriorityService.PriorityCoreLogic(disruptedFlightDB, PNR, FindThePNR_Priority, rule_PNR_PriorityTables, booking, FlightType);
                            }
                        }
                    });
                    await Task.WhenAll(tasks);
                }
                return FindThePNR_Priority;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"databasePriorityAndFliterOut"}{" msg:-"} {ex.Message}:- {"End "}");
                await _logHelper.LogConsoleException(ex);
                throw new InvalidOperationException(ex.Message);
            }
        }


        private async Task<HashSet<string>> GetDistinctPNR(string token, string LegKey)
        {
            try
            {
                var recordLocators = new ConcurrentDictionary<string, byte>();
                await _logHelper.LogInfo("Get The PNR Using this LegKey : " + LegKey);
                var request = new HttpRequestMessage(HttpMethod.Get, $"{LegKey}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                    _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
                var response = await _httpClient.SendAsync(request);
                string result = await response.Content.ReadAsStringAsync();
                ManifestLegDetailsResponseModel? manifestPNRDetails = JsonConvert.DeserializeObject<ManifestLegDetailsResponseModel>(result);
                Parallel.ForEach(manifestPNRDetails.Data.Passengers, item =>
                {
                    recordLocators.TryAdd(item.RecordLocator, 0);
                });
                return new HashSet<string>(recordLocators.Keys);
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"GetDistinctPNR"}{" msg:-"} {ex.Message}:- {"End "}");
                await _logHelper.LogConsoleException(ex);
                throw;
            }
        }
        private async Task<List<SortedPNR>> GetDistinctUnshortedPNR(DisruptedFlightDB disruptedFlightsModelResponse, IBookingService _bookingService, string LegKey, string Token)
        {
            try
            {
                List<SortedPNR> FindThePNR_Priority = new List<SortedPNR>();
                HashSet<string> PNRSet = new HashSet<string>();
                await _logHelper.LogInfo("Get The PNR Using this LegKey : " + LegKey);
                var request = new HttpRequestMessage(HttpMethod.Get, $"{LegKey}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
                if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                    _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
                var response = await _httpClient.SendAsync(request);
                string result = await response.Content.ReadAsStringAsync();
                ManifestLegDetailsResponseModel? manifestPNRDetails = JsonConvert.DeserializeObject<ManifestLegDetailsResponseModel>(result);
                foreach (var item in manifestPNRDetails.Data.Passengers)
                {
                    if (!PNRSet.Contains(item.RecordLocator))
                    {
                        var bookingDetails = await _bookingService.GetBookingByRecordLocator(item.RecordLocator, Token);
                        if (bookingDetails != null && bookingDetails != "")
                        {
                            BookingDetails? booking = JsonConvert.DeserializeObject<BookingDetails>(bookingDetails);
                            PNRSet.Add(item.RecordLocator);
                            FindThePNR_Priority.Add(await _pNRPriorityService.bindshortPNRList(booking, disruptedFlightsModelResponse, item.RecordLocator, 1, 1));
                        }
                    }
                }
                return FindThePNR_Priority;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- GetDistinctUnshortedPNR"}{" msg:-"} {ex.Message}:- {":- End "}");
                await _logHelper.LogConsoleException(ex);
                throw new InvalidOperationException(ex.Message);
            }
        }
        
    }
}
