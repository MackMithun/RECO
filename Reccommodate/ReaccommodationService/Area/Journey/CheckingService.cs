using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface;
using System.Net.Http.Headers;
using System.Text;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey
{
    public class CheckingService : ICheckingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        public CheckingService(HttpClient httpClient, ILogHelper logHelper, IConfiguration configuration)
        {
            _logHelper = logHelper; 
            _httpClient = httpClient;   
            _configuration = configuration; 
        }
        public async Task<bool> checkOutJourney(BookingDetails bookingDetails,string Token)
        {
            try
            {
                checkOutRequest checkOutRequest = new checkOutRequest
                {
                    Passengers = new List<PassengerKey>()
                };
                var passengerSegmentsWithLiftStatus1 = bookingDetails?.Data?.Journeys?[0].Segments?.SelectMany(segment => segment.passengerSegment.Values)?
                    .Where(passengerSegment => passengerSegment.liftStatus == 1)?.ToList();
                foreach(var PassengerKey in passengerSegmentsWithLiftStatus1)
                {
                    string? passengerKey = PassengerKey?.passengerKey ?? "";
                    if (!string.IsNullOrEmpty(passengerKey))
                    {
                        checkOutRequest.Passengers?.Add(new PassengerKey { passengerKey = passengerKey });
                    }
                }
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(checkOutRequest);
                HttpResponseMessage response = await HTTPMethod(Token, jsonPayload, $"journey/{bookingDetails?.Data?.Journeys?[0].JourneyKey??""}");
                if(response.IsSuccessStatusCode)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"checkOutJourney"} :- {"End msg :" + ex.Message}");
                return false;
            }
        }
        private async Task<HttpResponseMessage> HTTPMethod(string? Token, string jsonPayload,string Endpoint)
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri(_httpClient.BaseAddress +Endpoint),
                Content = content
            };
            if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return response;
        }
    }
}
