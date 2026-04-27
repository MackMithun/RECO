using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface
{
    public interface ICheckingService
    {
        Task<bool> checkOutJourney(BookingDetails bookingDetails, string Token);
    }
}
