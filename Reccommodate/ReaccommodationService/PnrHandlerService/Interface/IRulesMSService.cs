namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface IRulesMSService
    {
        Task<Dictionary<string, List<int>>> GetPriorityDataList();
        Task<List<string>> GetRestrictedSSRList();
        Task<List<string>> GetStakeHolderaList();
    }
}
