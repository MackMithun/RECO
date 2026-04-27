using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface
{
    public interface IMoveAvailabilityServiceForNextDays
    {
        Task<ToJourneyDetails?> ToJourneyKey(Reaccommodation_Model? reaccommodationMs, string token);
    }
}
