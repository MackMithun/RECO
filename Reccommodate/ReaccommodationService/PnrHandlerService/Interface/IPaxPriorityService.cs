using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface IPaxPriorityService
    {
        Task<bool> LOY_GRP(BookingDetails bookingDetails);
        Task<bool> LOY_SSRWCHR(BookingDetails bookingDetails);
        Task<bool> LOY_SSRFamily(BookingDetails bookingDetails);
        Task<bool> LOY(BookingDetails bookingDetails);
        Task<bool> WCHR(BookingDetails bookingDetails);
        Task<bool> UMNR(BookingDetails bookingDetails);
        Task<bool> GROUP(BookingDetails bookingDetails);
        Task<bool> SME(BookingDetails bookingDetails);
        Task<bool> CORPORATE(BookingDetails bookingDetails);
        Task<bool> Infant(BookingDetails bookingDetails);
        Task<bool> CHILD(BookingDetails bookingDetails);
        Task<bool> FAMILY(BookingDetails bookingDetails);
    }
}
