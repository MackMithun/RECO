using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.DatabaseModel;
using RECO.Reaccommodation_MS.Models.Enum;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class PNRPriorityService : IPNRPriorityService
    {
        private IPaxPriorityService _paxPriorityService;
        private ILogHelper _logHelper;
        public PNRPriorityService(ILogHelper logger, IPaxPriorityService paxPriorityService)
        {
            _logHelper = logger;
            _paxPriorityService = paxPriorityService;       
        }
        public async Task PriorityCoreLogic(DisruptedFlightDB disruptedFlightsModelResponse, string PNR, List<SortedPNR> FindThePNR_Priority, List<Rule_PNR_PriorityTable> rule_PNR_PriorityTables, BookingDetails? booking, int FlightType)
        {
            try
            {
                if (await _paxPriorityService.LOY_GRP(booking))
                   await SetPriorityCoreLogic(FindThePNR_Priority, rule_PNR_PriorityTables, booking, disruptedFlightsModelResponse, PNR, FlightType, Enum_PaxType.LOY_GRP.ToString());
                else if (await _paxPriorityService.LOY_SSRWCHR(booking))
                    await SetPriorityCoreLogic(FindThePNR_Priority, rule_PNR_PriorityTables, booking, disruptedFlightsModelResponse, PNR, FlightType, Enum_PaxType.LOY_SSR.ToString());
                else if (await _paxPriorityService.LOY_SSRFamily(booking))
                    await SetPriorityCoreLogic(FindThePNR_Priority, rule_PNR_PriorityTables, booking, disruptedFlightsModelResponse, PNR, FlightType, Enum_PaxType.LOY_F.ToString());
                else if (await _paxPriorityService.LOY(booking))
                    await SetPriorityCoreLogic(FindThePNR_Priority, rule_PNR_PriorityTables, booking, disruptedFlightsModelResponse, PNR, FlightType, Enum_PaxType.LOY.ToString());
                else if (await _paxPriorityService.WCHR(booking))
                    await SetPriorityCoreLogic(FindThePNR_Priority, rule_PNR_PriorityTables, booking, disruptedFlightsModelResponse, PNR, FlightType, Enum_PaxType.WCHR.ToString());
                else if (await _paxPriorityService.UMNR(booking))
                    await SetPriorityCoreLogic(FindThePNR_Priority, rule_PNR_PriorityTables, booking, disruptedFlightsModelResponse, PNR, FlightType, Enum_PaxType.UMNR.ToString());
                else if (await _paxPriorityService.GROUP(booking))
                    await SetPriorityCoreLogic(FindThePNR_Priority, rule_PNR_PriorityTables, booking, disruptedFlightsModelResponse, PNR, FlightType, Enum_PaxType.GROUP.ToString());
                else if (await _paxPriorityService.SME(booking))
                    await SetPriorityCoreLogic(FindThePNR_Priority, rule_PNR_PriorityTables, booking, disruptedFlightsModelResponse, PNR, FlightType, Enum_PaxType.SME.ToString());
                else if (await _paxPriorityService.CORPORATE(booking))
                    await SetPriorityCoreLogic(FindThePNR_Priority, rule_PNR_PriorityTables, booking, disruptedFlightsModelResponse, PNR, FlightType, Enum_PaxType.CORPORATE.ToString());
                else if (await _paxPriorityService.Infant(booking))
                    await SetPriorityCoreLogic(FindThePNR_Priority, rule_PNR_PriorityTables, booking, disruptedFlightsModelResponse, PNR, FlightType, Enum_PaxType.Infant.ToString());
                else if (await _paxPriorityService.CHILD(booking))
                    await SetPriorityCoreLogic(FindThePNR_Priority, rule_PNR_PriorityTables, booking, disruptedFlightsModelResponse, PNR, FlightType, Enum_PaxType.CHILD.ToString());
                else if (await _paxPriorityService.FAMILY(booking))
                    await SetPriorityCoreLogic(FindThePNR_Priority, rule_PNR_PriorityTables, booking, disruptedFlightsModelResponse, PNR, FlightType, Enum_PaxType.FAMILY.ToString());
                else
                    await SetPriorityCoreLogic(FindThePNR_Priority, rule_PNR_PriorityTables, booking, disruptedFlightsModelResponse, PNR, FlightType, Enum_PaxType.ANY.ToString());
            }
            catch (Exception ex)
            {
                await _logHelper.LogConsoleException(ex);
            }
        }
        private async Task SetPriorityCoreLogic(List<SortedPNR> FindThePNR_Priority, List<Rule_PNR_PriorityTable> rule_PNR_PriorityTables, BookingDetails booking, DisruptedFlightDB disruptedFlightsModelResponse, string PNR, int FlightType, string paxType)
        {
            Rule_PNR_PriorityTable? getTheSSRList = rule_PNR_PriorityTables.FirstOrDefault(x => x.PaxType == paxType);
            int Priority = 12;
            if (getTheSSRList != null && getTheSSRList?.Priority!=null)
            {
                Priority = getTheSSRList?.Priority??12;
            }
            FindThePNR_Priority.Add(await bindshortPNRList(booking, disruptedFlightsModelResponse, PNR, FlightType, Priority));
        }
        public async Task<SortedPNR> bindshortPNRList(BookingDetails booking, DisruptedFlightDB disruptedFlightsModelResponse, string? PNR, int FlightType,int priority)
        {
            try
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
                return new SortedPNR
                {
                    FLTID = disruptedFlightsModelResponse.FLTID,
                    PNRCode = PNR,
                    EmailId = AesEncryption.AesEncrypt(EmailAddress.Trim()),
                    ContactNo = AesEncryption.AesEncrypt(PhoneNumbers.Trim()),
                    RECOStatus = Enum_PNR.Failed.ToString(),
                    ReasonForFailure = "",
                    Priority = priority,
                    FlightType = FlightType,
                    CreatedBy = disruptedFlightsModelResponse.CreatedBy,
                };
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- bindshortPNRList"}{" msg:-"} {ex.Message}:- {"End "}");
                await _logHelper.LogError(ex.Message);
                return new SortedPNR();
            }
        }
    }
}
