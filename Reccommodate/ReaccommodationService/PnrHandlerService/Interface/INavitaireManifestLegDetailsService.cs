using RECO.Reaccommodation_MS.Models.DatabaseModel;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;
namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface INavitaireManifestLegDetailsService
    {
        Task<List<SortedPNR>> GetListShortedPNRAsync(DisruptedFlightDB disruptedFlightDB, IBookingService bookingService, IDataMsService _dataMsService, string LegKey, string Token);
        Task<List<SortedPNR>> GetListUnShortedPNRAsync(DisruptedFlightDB disruptedFlightDB, IBookingService _bookingService, string LegKey, string Token);
    }
}