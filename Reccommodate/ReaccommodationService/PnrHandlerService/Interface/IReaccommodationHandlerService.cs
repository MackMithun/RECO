using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.Models.ResponseModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface IReaccommodationHandlerService
    {
        Task<PNRDetails> ReaccomodateToSpecificFlight(Reaccommodation_Model reaccommodationMs, string Token);
        Task<PNRDetails> ReaccomodateToSuitableFlight(Reaccommodation_Model reaccommodationMs, string Token);
    }
}
