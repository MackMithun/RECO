using Newtonsoft.Json;
using RECO.DistrubtionHandler_MS.IUtilities;
using RECO.DistrubtionHandler_MS.Models.ResponseModel.NHub;
using RECO.DistrubtionHandler_MS.UCGHandlerService.Interface;

namespace RECO.DistrubtionHandler_MS.UCGHandlerService
{
    public class NotificationHUBService : INotificationHUBService
    {
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _caller = typeof(NotificationHUBService).Name;
        public NotificationHUBService(IConfiguration configuration, ILogHelper logHelper, HttpClient httpClient)
        {
            _configuration = configuration;
            _logHelper = logHelper;
            _httpClient = httpClient;
        }
        public async Task<NotificationHUBResponse> SendTheNotify(notificationTemplateHUB TemplateJSON, int id)
        {
            _logHelper.LogInfo($"{_caller} SendTheNotify :- Start For ID:{id}");
            var userKey = _configuration.GetValue<string>("NotifyGatewayKey");
            var Endpoint = _configuration.GetValue<string>("NotificationHUB");
            var apiUrl = $"{Endpoint}/pps/message-preprocessor/api/v1/message/trigger?user_key={userKey}&id={id}";
            using (var httpClient = new HttpClient())
            {
                try
                {
                    using (var formData = new MultipartFormDataContent())
                    {
                        string jsonBody = JsonConvert.SerializeObject(TemplateJSON);
                        formData.Add(new StringContent(jsonBody), "data");
                        var response = await httpClient.PostAsync(apiUrl, formData);
                        if (response.IsSuccessStatusCode)
                        {
                            var responseData = await response.Content.ReadAsStringAsync();
                            responseData = responseData.Replace("[UTC]", "");
                            NotificationHUBResponse? notificationHUBResponse = JsonConvert.DeserializeObject<NotificationHUBResponse>(responseData);
                            if (notificationHUBResponse != null)
                            {
                                _logHelper.LogInfo($"{_caller} SendTheNotify :- END For ID:{id}{"IsSuccessStatusCode:"}{response.IsSuccessStatusCode}{" requestId :"}{notificationHUBResponse.RequestId}");
                                return notificationHUBResponse;
                            }
                        }
                        return new NotificationHUBResponse();
                    }
                }
                catch (Exception ex)
                {
                    _logHelper.LogConsoleException(ex);
                    return new NotificationHUBResponse();
                }
            }
        }
    }
}
