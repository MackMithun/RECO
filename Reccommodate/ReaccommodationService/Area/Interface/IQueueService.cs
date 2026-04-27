namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface
{
    public interface IQueueService
    {
        Task<bool> fnDeleteQueue(string OldQueue, string Token);
        Task<bool> fnCreateQueue(string Token);
    }
}
