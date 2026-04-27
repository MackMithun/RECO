using RECO.DistrubtionHandler_MS.Models.RequestModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel;

namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface
{
    public interface ICOBRosterService
    {
        Task<List<CrewMember>> GetCOBRoster(DisruptedFlight disruptedFlight);
    }
}
