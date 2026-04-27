using RECO.Reaccommodation_MS.Models.DatabaseModel;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.ManifestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.MCT;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface IDataMsService
    {
        Task<List<Rule_PNR_PriorityTable>> GetRulePNRPriorityList();
        Task<Dictionary<string, string>> GetAppParameterList();
        Task<List<string>> GetExceptionList(DisruptedFlightRequest disruptedFlightRequestpublic ,Dictionary<string, string?>? OriginDestination);
        Task<DisruptedFlightDB> GetDisruptedFlight(DisruptedFlight disruptedFlight);
        Task<bool> AddPNRDetails(List<SortedPNR> sortedPNR);
        Task<List<PNRDetail>> GetPNRDetails(DisruptedFlightDB? disruptedFlightDB);
        Task<string> UpdatePNRDetails(List<PNRDetail> pnrDetail);
        Task<string> UpdateDisruptedflights(UpdateDisruptedFlights updateDisruptedFlights);
        Task<string> PostRoutingDetails(List<RoutingDetail> routingDetail);
        Task<double> CheckMCTConnectionTime(string StationCode, string flightType);
        Task<MCT_ImpactedFlight> GetStationConnectionRuleList(MCT_ImpactedFlight impactedFlightModel);
        Task<MCT_ImpactedFlight> StationConnectionTimeList(MCT_ImpactedFlight impactedFlightModel);
        Task<NhbTemplateResponse> GetNhbTemplate();
        Task<FetchDashboardResponse> fetchDashboardDetails(FetchDashboardRequest dashboardDetails);
    }
}
