using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.LegModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.ManifestModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface IPNRHandlerService
    {
        Task<ShortPNRModel?> GetSortedPNR(Reaccommodation_Model reaccommodationMs, string Token);
        Task<ShortPNRModel?> GetUnSortedPNR(Reaccommodation_Model reaccommodationMs, string Token);
        Task<ManifestDetails> GetManifestDetails(DisruptedFlightRequest disruptedFlightRequest, string Token);
    }
}
