using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.Models.ResponseModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface
{
    public interface IUpdateJourney
    {
        Task<PNRDetails> SetUpdateJourney(Reaccommodation_Model? reaccommodation_Model, string token);
    }
}
