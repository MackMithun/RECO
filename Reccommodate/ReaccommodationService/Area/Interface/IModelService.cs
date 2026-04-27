using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Model;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface
{
    public interface IModelService
    {
        Task<UpdateJourneyModel?> UpdateMoveJourneyRequest(JourneyRequestModel? journeyRequest);
        Task<TripMoveAvailabilityRequest> GetMoveavailabilityRequest(BookingDetails bookingDetails,int NextAddDays);
    }
}
