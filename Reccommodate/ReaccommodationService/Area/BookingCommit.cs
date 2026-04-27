using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;
using System.Net.Http.Headers;
using System.Text;
using RECO.Reaccommodation_MS.IUtilities;
using System.Text.Json;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Model;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area
{
    public class BookingCommit : IBookingCommit
    {
        private readonly HttpClient _httpClient;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        private readonly string _caller = typeof(BookingCommit).Name;
        public BookingCommit(HttpClient httpClient, ILogHelper logHelper, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logHelper = logHelper;
            _configuration = configuration;
        }
        public async Task<bool> SetBookingallowConcurrentChanges(string _token, string CommitComment)
        {
            try
            {
                bool _status = false;
                BookingCommitModel request = new BookingCommitModel
                {
                    LatestReceivedBy = "INET",
                    LatestReceivedReference = "",
                    RestrictionOverride = false,
                    WaiveNameFee = false,
                    WaivePenalityFee = false,
                    WaiveSpoilageFee = false,
                    DistributeToContacts = false,
                    NotifyContacts = true,
                    Comments = new List<Comment1>
                    {
                        new Comment1 { Type = 2, Text = CommitComment }
                    }
                };
                var jsonPayload = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                    _httpClient.DefaultRequestHeaders.Add("user_key", _configuration.GetValue<string>("NAV_GatewayKey"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

                var response = await _httpClient.PutAsync("", content);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _status = true;
                }
                return _status;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{"SetBookingallowConcurrentChanges"} :- {"End msg " + ex.Message}");
                await _logHelper.LogConsoleException(ex);
                return false;
            }

        }
    }
}
