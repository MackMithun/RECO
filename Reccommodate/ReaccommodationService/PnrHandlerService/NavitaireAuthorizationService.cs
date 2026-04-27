using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;
using System.Text;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class NavitaireAuthorizationService : INavitaireAuthorizationService
    {
        private readonly AuthConfigurationRequest _authConfigurationRequestModel;
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        private readonly string _caller = typeof(NavitaireAuthorizationService).Name;
        public NavitaireAuthorizationService(IOptions<AuthConfigurationRequest> authConfigurationRequestModel, HttpClient httpClient, ILogHelper logHelper, IConfiguration configuration)
        {
            _authConfigurationRequestModel = authConfigurationRequestModel.Value;
            _httpClient = httpClient;
            _logHelper = logHelper;
            _configuration = configuration; 
        }
        public async Task<string> GetTokenAsync()
        {
            try
            {
                var authRequest = new
                {
                    applicationName = _authConfigurationRequestModel.ApplicationName,
                    credentials = new
                    {
                        username = _authConfigurationRequestModel?.Credentials?.Username,
                        alternateIdentifier = _authConfigurationRequestModel?.Credentials?.AlternateIdentifier,
                        password = _authConfigurationRequestModel?.Credentials?.Password,
                        domain = _authConfigurationRequestModel?.Credentials?.Domain
                    }
                };
                var jsonContent = JsonConvert.SerializeObject(authRequest);
              //  await _logHelper.LogInfo(" Login Credential : " + jsonContent);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                    _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
                var response = await _httpClient.PostAsync("", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                return tokenResponse?.Data?.Token??"";
            }

            catch (Exception ex)
            {
                await _logHelper.LogConsoleException(ex);
                throw new InvalidOperationException(ex.Message);
            }
        }
    }
    public class TokenResponse
    {
        public Data Data { get; set; }
    }

    public class Data
    {
        public string Token { get; set; }
        public int IdleTimeoutInMinutes { get; set; }
    }
}
