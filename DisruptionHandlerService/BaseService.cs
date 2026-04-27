using Newtonsoft.Json;
using RECO.DistrubtionHandler_MS.Common;
using RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.Models;
using System.Net;
using System.Text;

namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService
{
    public class BaseService : IBaseService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BaseService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public async Task<BaseResponse> SendAsync<T>(BaseRequests request, string? token = null) where T : class
        {
            try
            {
                HttpClient client = _httpClientFactory.CreateClient("RECO");
                HttpRequestMessage message = new();
                message.Headers.Add("Accept", "application/json");

                if (!string.IsNullOrEmpty(request.user_key))
                {
                    client.DefaultRequestHeaders.Add("user_key", request.user_key);
                }
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                }

                message.RequestUri = new Uri(request.Url);

                if (request.Data != null)
                {
                    message.Content = new StringContent(JsonConvert.SerializeObject(request.Data), Encoding.UTF8, "application/json");
                }

                HttpResponseMessage? apiResponse = null;

                switch (request.ApiType)
                {
                    case ApiType.POST:
                        message.Method = HttpMethod.Post;
                        break;

                    case ApiType.PUT:
                        message.Method = HttpMethod.Put;
                        break;

                    case ApiType.DELETE:
                        message.Method = HttpMethod.Delete;
                        break;

                    default:
                        message.Method = HttpMethod.Get;
                        break;
                }

                apiResponse = await client.SendAsync(message);

                switch (apiResponse.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        return new() { IsSuccess = false, Message = "Not Found" };

                    case HttpStatusCode.Forbidden:
                        return new() { IsSuccess = false, Message = "Access Denied" };

                    case HttpStatusCode.Unauthorized:
                        return new() { IsSuccess = false, Message = "Unauthorized" };

                    case HttpStatusCode.InternalServerError:
                        return new() { IsSuccess = false, Message = "Internal Server Error" };

                    default:
                        var apiContent = await apiResponse.Content.ReadAsStringAsync();
                        Type t = typeof(T);
                        if (!t.Name.Equals("Response"))
                        {
                            return new() { IsSuccess = true, Message = "Success", Result = apiContent };
                        }
                        else
                        {
                            var apiResponseDto = JsonConvert.DeserializeObject<T>(apiContent);
                            return (dynamic)apiResponseDto;
                        }
                }
            }
            catch (Exception ex)
            {

                var dto = new BaseResponse { Message = ex.Message.ToString(), IsSuccess = false };
                return dto;
            }
        }
    }
}

