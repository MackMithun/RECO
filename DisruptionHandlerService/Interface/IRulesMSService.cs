namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface
{
    public interface IRulesMSService
    {
        Task<List<string>> GetStakeHolderListAsync();
    }
}
