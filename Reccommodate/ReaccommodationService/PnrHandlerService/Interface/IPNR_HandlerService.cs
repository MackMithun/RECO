using RECO.Reaccommodation_MS.Models.DatabaseModel;
using RECO.Reaccommodation_MS.Models.ResponseModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface IPNR_HandlerService
    {
        Task<string> SaveDataAndNotify(PNRDetails pNRandAlternateFlightListModel, DisruptedFlightDB? disruptedFlightsModelResponse, Dictionary<string, string>? AppParameterList, NhbTemplateResponse nhbTemplate);
    }
}
