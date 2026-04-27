using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface
{
    public interface IDelay_ToJourneyKey
    {
        Task<ToJourneyDetails?> GetJourneyKey(Reaccommodation_Model? reaccommodationMs);
    }
}
