using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.Models.ResponseModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface
{
    public interface IMultipleJourneyService
    {
        Task<PNRDetails> SuitableflightforMultiplejourneys(Reaccommodation_Model? reaccommodationMs, string Token);
    }
}
