using Newtonsoft.Json;
using RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.Models.RequestModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel.TripModel;
using RECO.DistrubtionHandler_MS.IUtilities;
using System.Net.Http.Headers;
using System.Text;
using RECO.DistrubtionHandler_MS.Models.Enum;

namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService
{
    public class TripInfoLegsService : ITripInfoLegsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        public TripInfoLegsService(HttpClient httpClient, ILogHelper logHelper, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logHelper = logHelper;
            _configuration = configuration;
        }
        public async Task<TripInfoLegsResponseModel> GetManifest(NavitaireFlightRequest disruptedFlightRequestModel, string? Token)
        {
            try
            {
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(disruptedFlightRequestModel);
                HttpResponseMessage response = await HTTPMethod(Token, jsonPayload);
                string responseContent = await response.Content.ReadAsStringAsync();
                TripInfoLegsResponseModel? tripInfoLegsResponseModel = JsonConvert.DeserializeObject<TripInfoLegsResponseModel>(responseContent);
                if (tripInfoLegsResponseModel != null && tripInfoLegsResponseModel.Data != null && tripInfoLegsResponseModel.Data.Count() > 0)
                {
                    return tripInfoLegsResponseModel;
                }
                else
                {
                    throw new InvalidOperationException(Enum_CustomMessage.InvalidFlight.GetDescription());
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- GetManifest"} :- {ex?.Message??""}");
                throw new InvalidOperationException(ex?.Message??"");
            }
        }
        private async Task<HttpResponseMessage> HTTPMethod(string? Token, string jsonPayload)
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
            var response = await _httpClient.PostAsync("", content);
            return response;
        }
    }
}