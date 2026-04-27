using Newtonsoft.Json;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.DatabaseModel;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;
using System.Text;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class NotificationMS : INotificationMS
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly string _caller = typeof(NotificationMS).Name;
        public NotificationMS(HttpClient httpClient, IConfiguration configuration, ILogHelper logHelper)
        {
            _configuration = configuration;     
            _httpClient = httpClient;   
            _logHelper = logHelper; 
        }
        public async Task<bool> fnSendNotification(DisruptedFlightDB disruptedFlight)
        {
            try
            {
                string jsonBody = JsonConvert.SerializeObject(disruptedFlight);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                    _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("GatewayKey"));
                if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                    _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _configuration.GetValue<string>("InternalAuthorization"));
                await _logHelper.LogInfo(_configuration.GetSection("InternalMSAPI")["NotificationMS"]);
                var result = _httpClient.PostAsync(_configuration.GetSection("InternalMSAPI")["NotificationMS"], content);
                await _logHelper.LogInfo(result.Result.ToString());
                return true;    
            }
            catch (Exception ex) {
                await _logHelper.LogError($"{":- fnSendNotification"}{" msg:-"} {ex.Message}:- {":- End "}{JsonConvert.SerializeObject(disruptedFlight)}");
                return false; 
            }
        }
    }
}
