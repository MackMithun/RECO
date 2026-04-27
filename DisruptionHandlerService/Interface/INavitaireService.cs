using RECO.DistrubtionHandler_MS.Models.RequestModel;

namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface
{
    public interface INavitaireService
    {
        Task<List<DateTime>> VerifySTDInNavitaire(NavitaireFlightRequest navitaireFlightRequest);
        Task<ListOfHandleDiscruption> GetNavitaireFlight(NavitaireFlightRequest tripInfo, Dictionary<string, string> appParameterTable);
    }
}