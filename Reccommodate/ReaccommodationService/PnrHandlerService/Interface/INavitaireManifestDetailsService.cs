using RECO.Reaccommodation_MS.Models.ResponseModel.ManifestModel;
namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface INavitaireManifestDetailsService
    {
        Task<ManifestDetails> GetManifestDetailsAsync(DisruptedFlightRequest disruptedFlightRequestModel, string? Token);
    }
}