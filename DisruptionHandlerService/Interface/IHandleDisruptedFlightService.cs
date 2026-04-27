using RECO.DistrubtionHandler_MS.Models.DatabaseModel;
using RECO.DistrubtionHandler_MS.Models.RequestModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel;

namespace RECO.DistrubtionHandler_MS.DistrubtionHandlerService.Interface
{
    public interface IHandleDisruptedFlightService
    {
        Task<FlightDetails> GetFlightDetails(FlightData flightData, string token);
        Task<RecoFlight> CheckEligibility(DisruptedFlight disruptedFlight);
        Task<List<DisruptedFlight>> DisruptedFlight();
    }
}