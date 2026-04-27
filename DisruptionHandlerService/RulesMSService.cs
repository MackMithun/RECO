using Newtonsoft.Json.Linq;
using RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.IUtilities;

namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService
{
    public class RulesMSService : IRulesMSService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        public RulesMSService(HttpClient httpClient, ILogHelper logHelper, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logHelper = logHelper;
            _configuration = configuration;
        }
        public async Task<List<string>> GetStakeHolderListAsync()
        {
            try
            {
                if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                    _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("GatewayKey"));
                if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                    _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _configuration.GetValue<string>("InternalAuthorization"));
                HttpResponseMessage response = await _httpClient.GetAsync(_httpClient.BaseAddress + "/GetRestrictedSSR");
                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    JObject jsonObject = JObject.Parse(jsonString);
                    JArray stringArray = (JArray)jsonObject["stakeHolders"]["emailIDs"];
                    List<string> stringList = stringArray.ToObject<List<string>>();
                    return stringList;
                }
                else
                {
                    _logHelper.LogInfo($"{" :- GetStakeHolderListAsync"} :- {response.IsSuccessStatusCode}");
                    throw new InvalidOperationException("Rules MS: Get StakeHolder Table is Failed");
                }
            }
            catch (Exception ex) {
                _logHelper.LogError($"{" :- GetStakeHolderListAsync"} :- {ex?.Message ?? ""}");
                throw new InvalidOperationException(ex.Message);
            }
        }
    }
}
