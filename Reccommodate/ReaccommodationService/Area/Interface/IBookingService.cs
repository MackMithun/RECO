namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface
{
    public interface IBookingService
    {
        Task<string> GetBookingByRecordLocator(string pnr, string token);
    }
}
