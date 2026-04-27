namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface
{
    public interface IBookingCommit
    {
        Task<bool> SetBookingallowConcurrentChanges(string _token, string CommitComment);
    }
}
