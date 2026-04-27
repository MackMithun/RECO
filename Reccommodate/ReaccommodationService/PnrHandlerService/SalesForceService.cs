using Microsoft.Extensions.Options;
using RECO.Reaccommodation_MS.Models.RequestModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class SalesForceService : ISalesForceService
    {
        private readonly HttpClient _httpClient;
        private readonly SalesforceOptions _options;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration; 
        private readonly string _caller = typeof(SalesForceService).Name;

        public SalesForceService(HttpClient httpClient, IOptions<SalesforceOptions> options, ILogHelper logHelper, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logHelper = logHelper;
            _configuration = configuration; 
        }

        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                string baseUrl = _options.TokenUrl;
                string grantType = _options.GrantType;
                string clientId = _options.ClientId;
                string clientSecret = _options.ClientSecret;
                var queryParams = $"?grant_type={Uri.EscapeDataString(grantType)}&client_id={Uri.EscapeDataString(clientId)}&client_secret={Uri.EscapeDataString(clientSecret)}";
                string url = baseUrl + queryParams;
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("user_key", _options.user_key);
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        await _logHelper.LogInfo($"{_caller}:{":- GetAccessTokenAsync"} :- {"StatusCode:- "} {response.StatusCode}");
                        throw new Exception($"Failed to retrieve access token. StatusCode: {response.StatusCode}");
                    }
                    else
                    {
                        var responseStream = await response.Content.ReadAsStreamAsync();
                        var tokenResponse = await JsonSerializer.DeserializeAsync<SalesforceTokenResponse>(responseStream);
                        return tokenResponse?.access_token??"";
                    }
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{":- GetAccessTokenAsync"} :- {ex.Message}");
                await _logHelper.LogConsoleException(ex);
                return "";
            }
        }
        public async Task<string> ExecuteCompositeRequestInternalAsync(PNRDetail item, string accessToken)
        {
            try
            {
                string ENV=_configuration.GetValue<string>("TestKey");
                string recordTypeId = "012DQ0000000A4rYAE";
                if(ENV == "This is prod environment.")
                {
                    recordTypeId = "";
                }
                await _logHelper.LogInfo($"{_caller}:{":- ExecuteCompositeRequestInternalAsync"} :- {"Start"}{"ENV : "}{ENV}");
                await _logHelper.LogInfo($"{_caller}:{":- ExecuteCompositeRequestInternalAsync"} :- {"Start"}{"recordTypeId :"}{recordTypeId}");
                object compositeRequestBody = await fnbindSalesForceRequestData(item, recordTypeId);
                var jsonPayload = JsonSerializer.Serialize(compositeRequestBody);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                if (!_httpClient.DefaultRequestHeaders.Contains("user_key"))
                    _httpClient.DefaultRequestHeaders.Add("user_key", _options.user_key);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.PostAsync($"{_options.InstanceUrl}", content);
                var result = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return "Failed to execute composite request. StatusCode: {response.StatusCode}";
                }
                else
                {
                    await _logHelper.LogInfo($"{_caller}:{"ExecuteCompositeRequestInternalAsync"} :- {"End:- "} PNR :- "+ item.PNRCode +JsonSerializer.Serialize(result));
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{" :- ExecuteCompositeRequestInternalAsync:- "} :- {ex.Message}{":- PNR -: "+item?.PNRCode ??""}");
                await _logHelper.LogConsoleException(ex);
                throw;
            }
        }
        private async Task<object> fnbindSalesForceRequestData(PNRDetail pNRTableModel,string recordType)
        {
            try
            {
                string PhoneNo = AesEncryption.AesDecrypt(pNRTableModel.ContactNo);
                string Email = AesEncryption.AesDecrypt(pNRTableModel.EmailId);
                if(Email.Contains(","))
                {
                    List<string> Emaillist= Email.Split(",").ToList();
                    Email= Emaillist[0];    
                }
                string descriptionJson = JsonSerializer.Serialize(pNRTableModel);
                var compositeRequest = new
                {
                    allOrNone = true,
                    compositeRequest = new object[]
                    {
         new
         {
             method = "PATCH",
             url = "/services/data/v59.0/sobjects/Account/Caller_Number__c/"+PhoneNo,
             referenceId = "NewCustomer",
             body = new
             {
                 Salutation = (string)null,
                 FirstName = (string)null,
                 MiddleName = (string)null,
                 LastName = "IndiGo Customer",
                 PersonEmail = Email,
                 PersonMobilePhone = PhoneNo,
                 recordTypeId = recordType
             },
             httpHeaders = (object)null
         },
         new
         {
             method = "GET",
             url = "/services/data/v59.0/queryAll?q=SELECT+Id,+PersonContactId+FROM+Account+WHERE+Id='@{NewCustomer.id}'",
             referenceId = "CustomerData",
             httpHeaders = (object)null
         },
         new
         {
             method = "POST",
             referenceId = "NewCase",
             url = "/services/data/v59.0/sobjects/Case",
             body = new
             {
                 AccountId = "@{CustomerData.records[0].Id}",
                 ContactId = "@{CustomerData.records[0].PersonContactId}",
                 Description = "["+descriptionJson+"]",
                 Subject = "Sub - RECO PNR :"+pNRTableModel.PNRCode+" Re-accommodation not done",
                 Origin = "Web",
                 Status = "Open",
                 PNR_No__c = pNRTableModel.PNRCode,
                 Amount__c = 320,
                 JusPay_Id__c = "JPAYeb9f33c049ba65557",
                 SuppliedEmail = "pankaj.s@techmatrixconsulting.com"
             },
             httpHeaders = new
             {
                 Sforce_Auto_Assign = "FALSE"
             }
         }
                    }
                };
                return compositeRequest;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{":- fnbindSalesForceRequestData"} :- {ex.Message}{":- PNR:- "}{pNRTableModel?.PNRCode??""}");
                return ex.Message;
            }
        }
    }
}
