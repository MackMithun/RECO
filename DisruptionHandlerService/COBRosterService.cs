using AutoMapper;
using Newtonsoft.Json;
using RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.IUtilities;
using RECO.DistrubtionHandler_MS.Models.RequestModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel;
using System.Text;

namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService
{
    public class COBRosterService: ICOBRosterService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly string _caller = typeof(COBRosterService).Name;
        public COBRosterService(HttpClient httpClient, ILogHelper logHelper, IConfiguration configuration, IMapper mapper)
        {
            _httpClient = httpClient;
            _logHelper = logHelper;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<List<CrewMember>> GetCOBRoster(DisruptedFlight disruptedFlight)
        {
            try
            {
                COBRosterRequest request = _mapper.Map<COBRosterRequest>(disruptedFlight);
                var response = await PostJsonAsync<COBRosterRequest>("crewstandbydetails/api/CrewRosterDataForPFMC", request);
                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    List<CrewMember>? crewMember = JsonConvert.DeserializeObject<List<CrewMember>>(result);
                    if (crewMember != null && crewMember.Count()>0)
                    {
                        return crewMember;
                    }
                }
                _logHelper.LogInfo($"{_caller}{":- GetCOBRoster:-"}{"Status :-"}{response.IsSuccessStatusCode}{":- Flight No:-"+ disruptedFlight?.Identifier??""}");
                return new List<CrewMember>();
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{_caller}{"GetCOBRoster:-"}{ex.Message}{":- Flight No:-" + disruptedFlight?.Identifier ?? ""}");
                return new List<CrewMember>();
            }
        }
        private async Task<HttpResponseMessage> PostJsonAsync<T>(string endpoint, T data)
        {
            string jsonBody = JsonConvert.SerializeObject(data);
            _logHelper.LogInfo($"{_caller}{"endpoint:-"}{"JSON :-"}{jsonBody}");
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("COBGatewayKey"));
            _logHelper.LogInfo($"{"6.1.PostJsonAsync : "}" + endpoint + "Json = " + jsonBody);
            return await _httpClient.PostAsync(_httpClient.BaseAddress + endpoint, content);
        }
    }
}
