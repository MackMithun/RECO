namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface INavitaireAuthorizationService
    {
        Task<string> GetTokenAsync();
    }
}
