using Newtonsoft.Json;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.ResponseModel.ManifestModel;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class NavitaireManifestDetailsService : INavitaireManifestDetailsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        public NavitaireManifestDetailsService(HttpClient httpClient, ILogHelper logHelper, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logHelper = logHelper;
            _configuration = configuration; 
        }
        public async Task<ManifestDetails> GetManifestDetailsAsync(DisruptedFlightRequest disruptedFlightRequestModel, string? Token)
        {
            try
            {
                string? formattedDate = disruptedFlightRequestModel?.beginDate?.ToString("yyyy-MM-dd");
                var request = new HttpRequestMessage(HttpMethod.Get, $"manifest?Origin={disruptedFlightRequestModel.origin}&" +
                             $"Destination={disruptedFlightRequestModel.destination}&" +
                             $"CarrierCode={disruptedFlightRequestModel.carrierCode}&" +
                             $"BeginDate={formattedDate}&" +
                             $"Identifier={disruptedFlightRequestModel.identifier}&" +
                             $"FlightType={disruptedFlightRequestModel.flightType}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
                if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                    _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string responseContent = await response.Content.ReadAsStringAsync();
                ManifestDetails? manifestDetails = JsonConvert.DeserializeObject<ManifestDetails>(responseContent);
                return manifestDetails;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"GetManifestDetailsAsync"}{" msg:-"} {ex.Message}:- {"End "} -:- {JsonConvert.SerializeObject(disruptedFlightRequestModel)}");
                throw new InvalidOperationException(ex.Message);
            }

        }
    }
}
