using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface
{
    public interface IToJourneyKeyService
    {
        Task<ToJourneyDetails> GetKeyJourneyNavigateFlightDate(Reaccommodation_Model? reaccommodationMs, bool NextTry);
    }
}
