using RECO.DistrubtionHandler_MS.Models.DatabaseModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel.NHub;

namespace RECO.DistrubtionHandler_MS.UCGHandlerService.Interface
{
    public interface IUCGWebServices
    {
        string generateEmailTemplateForApprovalAsync(RecoFlight? recoFlight);
        string generateEmailTemplateAsync(RecoFlight? recoFlight);
        bool sendTheEmailAsync(RecoFlight? recoFlight,string emailTemplate);
        bool sendEmailByNHub(DisruptedFlightResponse? disruptionFlight, NhbTemplateResponse nhbTemplates, List<string>? ListOfEmailIDs, string TemplateName);
    }
}
