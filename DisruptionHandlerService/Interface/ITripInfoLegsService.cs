using RECO.DistrubtionHandler_MS.Models.RequestModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel.TripModel;

namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface
{
    public interface ITripInfoLegsService
    {
        Task<TripInfoLegsResponseModel> GetManifest(NavitaireFlightRequest disruptedFlightRequestModel, string? Token);
    }
}