using RECO.Reaccommodation_MS.Models;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface
{
    public interface IBaseService
    {
        Task<BaseResponse> SendAsync<T>(BaseRequests request, string? token = null) where T : class;
    }
}
