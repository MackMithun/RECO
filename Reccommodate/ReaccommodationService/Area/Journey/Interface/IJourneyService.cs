using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.MCT;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface
{
    public interface IJourneyService
    {
        Task<MCT_ImpactedFlight> SetMCTJourney(BookingDetails? booking, IEnumerable<Models.ResponseModel.BookingModel.Journey>? Tempbooking);
        Task<MCT_ImpactedJourney> CheckImpactedJourney(Reaccommodation_Model? reaccommodation_Model);
        Task<MCT_ImpactedJourney> CheckImpactedJourneyMCT(Reaccommodation_Model? reaccommodation_Model, MCT_ImpactedJourney impactedJourney);
        Task<string> checkSSR(Reaccommodation_Model? reaccommodationMs);
        Task<bool> ActionOnImpactedJourney(MCT_ImpactedJourney impactedJourney, string? DisruptedFlight, DateTime STD, DateTime STA, double _minTime);
        Task<PNRDetails> GetAccommodationByFlightNoResponseModel(BookingDetails? oldbooking, BookingDetails? newbooking, PNRDetail? pNRTableModel, bool StatusOfReaccommodation, string ReasonForFailure);
        Task<bool> checkMultiSegments(Reaccommodation_Model reaccommodationMs);
        Task<bool> CheckCodeShareFlight(BookingDetails? booking);
        Task<bool> CheckStretchPNR(BookingDetails? booking);
        Task<bool> HandleUndoCheckingTask(BookingDetails? booking, string Token);
        Task<bool> checkBoardedStatus(BookingDetails? booking);
    }
}
