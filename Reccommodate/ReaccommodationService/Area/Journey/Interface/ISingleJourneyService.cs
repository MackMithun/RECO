using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.Models.ResponseModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface
{
    public interface ISingleJourneyService
    {
        Task<PNRDetails> SuitableflightforSingleJourneys(Reaccommodation_Model? reaccommodationMs, string Token);
    }
}
