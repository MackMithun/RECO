using RECO.DistrubtionHandler_MS.Models.DatabaseModel;
using RECO.DistrubtionHandler_MS.Models.RequestModel;

namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface
{
    public interface IReaccommodationMSService
    {
        Task<IdentifierResponse> GetFlightDetails(Models.RequestModel.FlightData flightData, string token);
        Task<List<DisruptedFlight>> cronJobSchedule();
        Task<bool> InitiateReco(DisruptedFlight disruptedFlight);
        Task<bool> RetryRECO(DisruptedFlight disruptedFlight);

    }
}
