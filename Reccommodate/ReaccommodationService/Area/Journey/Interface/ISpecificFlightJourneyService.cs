using RECO.Reaccommodation_MS.Models.ResponseModel;
using RECO.Reaccommodation_MS.Models;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface
{
    public interface ISpecificFlightJourneyService
    {
        Task<PNRDetails> SpecificflightforSingleJourneys(Reaccommodation_Model? reaccommodationMs, string Token);
        Task<PNRDetails> SpecificflightforMultiplejourneys(Reaccommodation_Model? reaccommodationMs, string Token);
    }
}
