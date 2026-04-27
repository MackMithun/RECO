namespace RECO.DistrubtionHandler_MS.DistrubtionHandlerService.Interface
{
    public interface IAuthService
    {
        Task<string> GetTokenAsync();
    }
}