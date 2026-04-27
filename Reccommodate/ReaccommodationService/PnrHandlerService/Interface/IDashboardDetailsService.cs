using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface IDashboardDetailsService
    {
        Task fnGenrateDashboardDetails(DashboardDetails disruptedFlight);
        Task<DashboardResponse> fnDashboardDetails();
    }
}
