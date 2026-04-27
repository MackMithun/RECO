using Microsoft.Extensions.Options;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Model;
using System.Net.Http.Headers;
using System.Text;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area
{
    public class QueueService : IQueueService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly QueueModel _queueChangeRequest;
        private readonly IConfiguration _configuration;
        public QueueService(HttpClient httpClient, ILogHelper logHelper, IOptions<QueueModel> queueChangeRequest, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logHelper = logHelper;
            _queueChangeRequest = queueChangeRequest.Value;
            _configuration = configuration;
        }
        public async Task<bool> fnCreateQueue(string Token)
        {
            try
            {
                bool _status = false;
                CreateQueueRequest createQueueRequest = new CreateQueueRequest
                {
                    QueueCode = _queueChangeRequest.QueueCode,
                    Notes = _queueChangeRequest.Notes,
                    Password = _queueChangeRequest.Password,
                };
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(createQueueRequest);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                    _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                var response = await _httpClient.PostAsync("", content);
                var result = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _status = true;
                }
                return _status;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"fnCreateQueue"}{":- END "}{"msg"}{ex.Message}");
                return false;
            }
        }

        public async Task<bool> fnDeleteQueue(string OldQueue, string Token)
        {
            try
            {
                bool _status = false;
                DeleteQueueRequest createQueueRequest = new DeleteQueueRequest
                {
                    QueueCode = OldQueue,
                    Notes = _queueChangeRequest.Notes,
                    Password = _queueChangeRequest.decryptedPassword,
                    AuthorizedBy = _queueChangeRequest.AuthorizedBy,
                };
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(createQueueRequest);
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri(_httpClient.BaseAddress.ToString()),
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };
                if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                    _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _status = true;
                }
                return _status;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"fnDeleteQueue"}{":- END "}{"msg"}{ex.Message}");
                return false;
            }
        }
    }
}
