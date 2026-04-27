using RECO.DistrubtionHandler_MS.Models.ResponseModel.NHub;

namespace RECO.DistrubtionHandler_MS.UCGHandlerService.Interface
{
    public interface INotificationHUBService
    {
        Task<NotificationHUBResponse> SendTheNotify(notificationTemplateHUB TemplateJSON, int id);
    }
}
