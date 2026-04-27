using RECO.DistrubtionHandler_MS.Models.DatabaseModel;
using RECO.DistrubtionHandler_MS.Models.RequestModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel.NHub;

namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface
{
    public interface IDataMsService
    {
        Task<ExceptionFlightResponse?> GetExceptionList(NavitaireFlightRequest addDisruptedFlightsModel);
        Task<bool> PostdisruptedFlightDetails(DisruptedFlightsResponse disruptedFlightRequestModel);
        Task<bool> updatedisruptedFlightDetails(UpdateDisruptedFlights updateDisruptedFlightsModel);
        Task<DisruptedFlightResponse> CheckDisruptedFlightExists(DisruptedFlight disruptedFlight);        
        Task<Dictionary<string, string>> GetAppParameterList();
        Task<bool> PostHistoryDetails(DisruptedFlight disruptedFlight);
        Task<List<DisruptedFlightResponse>> ListOfDisruptedFlight();
        Task<bool> AddCrewMembers(List<CrewMember> crewMembers);
        Task<NhbTemplateResponse> GetNhbTemplate();
    }
}