using RECO.Reaccommodation_MS.Models.RequestModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface ISpecificFlightService
    {
        Task ReaccommodateToSpecificFlight(DisruptedFlight disruptedFlight, string Token);
    }
}
