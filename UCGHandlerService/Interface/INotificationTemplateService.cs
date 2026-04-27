using RECO.DistrubtionHandler_MS.Models.DatabaseModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel.NHub;

namespace RECO.DistrubtionHandler_MS.UCGHandlerService.Interface
{
    public interface INotificationTemplateService
    {
        Task<NhubTemplateJSON> fnGenrateTemplateJSON(DisruptedFlightResponse? disruptionFlight, NhbTemplateResponse nhbTemplates, List<string>? ListOfEmailIDs, string TemplateName);
    }
}
