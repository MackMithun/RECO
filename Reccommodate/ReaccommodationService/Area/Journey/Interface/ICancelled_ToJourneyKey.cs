using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface
{
    public interface ICancelled_ToJourneyKey
    {
        Task<ToJourneyDetails?> cancelGetKeyJourneyNavigateFlightDate(Reaccommodation_Model? reaccommodationMs, bool NextTry);
    }
}
