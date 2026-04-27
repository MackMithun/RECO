using RECO.Reaccommodation_MS.Models.DatabaseModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface INotificationMS
    {
        Task<bool> fnSendNotification(DisruptedFlightDB disruptedFlight);
    }
}
