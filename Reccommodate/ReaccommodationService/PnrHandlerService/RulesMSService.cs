using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RECO.Reaccommodation_MS.Common;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;
using System.Text;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class RulesMSService : IRulesMSService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        private readonly string _caller = typeof(RulesMSService).Name;
        public RulesMSService(HttpClient httpClient, ILogHelper logHelper, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logHelper = logHelper;
            _configuration = configuration;
        }
        public async Task<Dictionary<string, List<int>>> GetPriorityDataList()
        {
            try
            {
                HttpResponseMessage result = await PostGetAsync("/GetRestrictedSSR");
                if (result.IsSuccessStatusCode)
                {
                    var responseContent = await result.Content.ReadAsStringAsync();
                    JToken jsonObject = JObject.Parse(responseContent);
                    if (jsonObject is JObject)
                    {
                        var dataObject = jsonObject["alternateflightpriority"];
                        if (dataObject != null)
                        {
                            return JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(dataObject.ToString());
                        }
                    }
                    else if (jsonObject is JArray)
                    {
                        var dataArray = (JArray)jsonObject;
                        if (dataArray.Count > 0)
                        {
                            var dataobject = dataArray[0]["alternateflightpriority"];
                            if (dataobject != null)
                            {
                                return JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(dataobject.ToString());
                            }
                        }
                    }
                }
                await _logHelper.LogInfo($"{_caller}:{" :- GetPriorityDataList"} :- {result.IsSuccessStatusCode}");
                throw new InvalidOperationException("not get the flight priority list");
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}{":- GetPriorityDataList"}{" msg:-"} {ex.Message}:- {"End "}");
                throw new InvalidOperationException(ex.Message);
            }
        }

        public async Task<List<string>> GetRestrictedSSRList()
        {
            try
            {
                HttpResponseMessage response = await PostGetAsync("/GetRestrictedSSR");
                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    JObject jsonObject = JObject.Parse(jsonString);
                    JArray stringArray = (JArray)jsonObject["restrictedSSR"]["RestrictedSSRs"];
                    List<string> stringList = stringArray.ToObject<List<string>>();
                    return stringList;
                }
                else
                {
                    await _logHelper.LogInfo($"{_caller}:{" :- GetRestrictedSSRList"} :- {response.IsSuccessStatusCode}");
                    throw new InvalidOperationException("not get the restricted SSR list");
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}{"GetRestrictedSSRList"}{" msg:-"} {ex.Message}:- {" :- End "}");
                throw new InvalidOperationException(ex.Message);
            }
        }
        public async Task<List<string>> GetStakeHolderaList()
        {
            try
            {
                HttpResponseMessage response =await PostGetAsync("/GetRestrictedSSR");
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
                    await _logHelper.LogInfo($"{_caller}:{" :- GetStakeHolderaList"} :- {response.IsSuccessStatusCode}");
                    throw new InvalidOperationException("not get the stake holder list");
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}{"GetStakeHolderaList"}{" msg:-"} {ex.Message}:- {":- End "}");
                throw new InvalidOperationException(ex.Message);
            }
        }
        private async Task<HttpResponseMessage> PostGetAsync(string endpoint)
        {
            if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("GatewayKey"));
            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _configuration.GetValue<string>("InternalAuthorization"));
            return await _httpClient.GetAsync(_httpClient.BaseAddress + endpoint);
        }
    }
}
