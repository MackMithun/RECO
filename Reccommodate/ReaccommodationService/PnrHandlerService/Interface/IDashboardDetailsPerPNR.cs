using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface IDashboardDetailsPerPNR
    {
        Task<bool> fnGenrateDashboardPerPNR(DashboardDetails disruptedFlight,FetchDashboardResponse fetchDashboardResponse, Dictionary<string, string> AppParameterList, NhbTemplateResponse nhbTemplate, string _token);
    }
}
