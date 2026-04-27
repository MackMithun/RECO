using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area
{
    public class BookingService : IBookingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        private readonly string _caller = typeof(BookingService).Name;
        public BookingService(HttpClient httpClient, ILogHelper logHelper, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logHelper = logHelper;
            _configuration = configuration;
        }
        public async Task<string> GetBookingByRecordLocator(string pnr, string token)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{pnr}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                    _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                await _logHelper.LogInfo($"{_caller}:{"GetBookingByRecordLocator PNR :-" + pnr} :- {"End status :-" + response.IsSuccessStatusCode}");
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{"GetBookingByRecordLocator"}{" PNR :-"}{pnr}{":- msg :-"}{ex.Message} :- {"End"}");
                await _logHelper.LogConsoleException(ex);
                return "";
            }
        }
    }
}
