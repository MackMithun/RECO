using RECO.Reaccommodation_MS.Models.RequestModel;
namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface ISalesForceService
    {
        Task<string> GetAccessTokenAsync();
        Task<string> ExecuteCompositeRequestInternalAsync(PNRDetail item, string accessToken);
    }
}
