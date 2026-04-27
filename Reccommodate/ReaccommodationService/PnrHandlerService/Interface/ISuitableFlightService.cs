using RECO.Reaccommodation_MS.Models.RequestModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface ISuitableFlightService
    {
        Task ReaccommodateToSuitableFlight(DisruptedFlight disruptedFlight, string Token);
    }
}
