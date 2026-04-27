using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RECO.DistrubtionHandler_MS.DistrubtionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.Models.RequestModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel;
using RECO.DistrubtionHandler_MS.IUtilities;
using System.Text;

namespace RECO.DistrubtionHandler_MS.DistrubtionHandlerService
{
    public class AuthService : IAuthService
    {
        private readonly AuthConfigurationRequestModel _authConfigurationRequestModel;
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        public AuthService(IOptions<AuthConfigurationRequestModel> authConfigurationRequestModel, HttpClient httpClient, ILogHelper logHelper, IConfiguration configuration)
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
                _logHelper.LogError($"{" :- GetTokenAsync"} :- {ex.Message??""}");
                throw new InvalidOperationException(ex.Message);
            }
        }
    }
}