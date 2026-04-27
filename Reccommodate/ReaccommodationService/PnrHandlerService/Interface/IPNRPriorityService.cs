using RECO.Reaccommodation_MS.Models.DatabaseModel;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface IPNRPriorityService
    {
        Task<SortedPNR> bindshortPNRList(BookingDetails booking, DisruptedFlightDB disruptedFlightsModelResponse, string? PNR, int FlightType, int priority);
        Task PriorityCoreLogic(DisruptedFlightDB disruptedFlightsModelResponse, string PNR, List<SortedPNR> FindThePNR_Priority, List<Rule_PNR_PriorityTable> rule_PNR_PriorityTables, BookingDetails? booking, int FlightType);
    }
}
