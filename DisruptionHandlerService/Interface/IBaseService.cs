using RECO.DistrubtionHandler_MS.Models;

namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface
{
    public interface IBaseService
    {
        Task<BaseResponse> SendAsync<T>(BaseRequests request, string? token = null) where T : class;
    }
}