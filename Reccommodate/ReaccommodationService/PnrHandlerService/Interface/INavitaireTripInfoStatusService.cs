using RECO.Reaccommodation_MS.Models.ResponseModel.LegModel;
namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface INavitaireTripInfoStatusService
    {
        Task<TripinfoLegListModel?> GetLegKeyManifestDetailsAsync(TripInfoLegsRequest? disruptedFlightRequestModel, string? Token);
        Task<LegKeyStatusFlightInformationModel> GetLegKeyStatusAsync(Dictionary<string, string>  appParameterTable, string LegKey, string Token);
        Task<ShortPNRModel?> GetLegKeyManifestAsync(TripinfoLegListModel? tripinfoLegListModel);
    }
}