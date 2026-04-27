using Azure;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RECO.DistrubtionHandler_MS.IUtilities;
using RECO.DistrubtionHandler_MS.Models.DatabaseModel;
using RECO.DistrubtionHandler_MS.Models.Enum;
using RECO.DistrubtionHandler_MS.Models.ResponseModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel.NHub;
using RECO.DistrubtionHandler_MS.Models.UCGModel;
using RECO.DistrubtionHandler_MS.UCGHandlerService.Interface;
using System.Net;
using UCGWebService;

namespace RECO.DistrubtionHandler_MS.UCGHandlerService
{
    public class UCGWebServices : IUCGWebServices
    {
        private readonly UCG_CredentialsModel _uCG_CredentialsModel;
        private readonly INotificationHUBService _notificationHUBService;
        private readonly INotificationTemplateService _notificationTemplateService;
        private readonly ILogHelper _logHelper;
        public UCGWebServices(IOptions<UCG_CredentialsModel> uCG_CredentialsModel, ILogHelper logHelper, INotificationHUBService notificationHUBService,
           INotificationTemplateService notificationTemplateService)
        {
            _uCG_CredentialsModel = uCG_CredentialsModel.Value;           
            _logHelper = logHelper;
            _notificationHUBService = notificationHUBService;   
            _notificationTemplateService = notificationTemplateService; 
        }
        public string GenerateEmailXml(RecoFlight? recoFlight, string serverPath)
        {
            string? DisruptionType = recoFlight?.dbDistruptedFlight?.DisruptionType.ToString() == Enum_DisruptionType.FLtNotUpdatedinNav.ToString() ? recoFlight?.disruptedFlight?.EventType : recoFlight?.dbDistruptedFlight?.DisruptionType.ToString();
            _logHelper.LogInfo(serverPath);
            string htmlContent = File.ReadAllText(serverPath);
            htmlContent = htmlContent.Replace("@@flightNumber", recoFlight?.disruptedFlight?.Identifier?.ToString())
                                   .Replace("@@departure", (recoFlight?.disruptedFlight?.BeginDate ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss"))
                                   .Replace("@@arrival", (recoFlight?.disruptedFlight?.EndDate ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss"))
                                   .Replace("@@ETD", recoFlight?.dbDistruptedFlight?.ETD.HasValue == true ? recoFlight.dbDistruptedFlight.ETD.Value.ToString("dd-MM-yyyy HH:mm:ss") : "")
                                   .Replace("@@ETA", recoFlight?.dbDistruptedFlight?.ETA.HasValue == true ? recoFlight.dbDistruptedFlight.ETA.Value.ToString("dd-MM-yyyy HH:mm:ss") : "")
                                   .Replace("@@origin", recoFlight?.disruptedFlight?.OriginStations?[0].ToString())
                                   .Replace("@@destination", recoFlight?.disruptedFlight?.DestinationStations?[0].ToString())
                                   .Replace("@@status", DisruptionType)
                                   .Replace("@@Reason", recoFlight?.ReasonNotReaccommodation)
                                   .Replace("@@Source", recoFlight?.disruptedFlight?.Source?.ToUpper() == "KAFKA" ? "KAFKA(Jeppesen)" : recoFlight?.disruptedFlight?.Source)
                                   .Replace("@@CreatedBy", recoFlight?.disruptedFlight?.CreatedBy);
            string subject = "Env-" + recoFlight?.ENV + " " + "Flight : " + recoFlight?.disruptedFlight?.Identifier?.ToString() + " " + DisruptionType + " " + (recoFlight?.disruptedFlight?.BeginDate ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss");
            string emailXml = $@"<ROOT><EMAIL><EMAILID>[EMAILIDXXX]</EMAILID><TEMPLATEID></TEMPLATEID><EMAILSUBJECTLINE>{subject}</EMAILSUBJECTLINE><MESSAGE>{htmlContent}</MESSAGE><APPID>{_uCG_CredentialsModel.UCG_APPID}</APPID><FLIGHTDT></FLIGHTDT><FLIGHTNO>{recoFlight?.disruptedFlight?.Identifier ?? ""}</FLIGHTNO><DEP>{recoFlight?.disruptedFlight?.DestinationStations?[0] ?? ""}</DEP><ARR>{recoFlight?.disruptedFlight?.OriginStations?[0] ?? ""}</ARR><PREDEPT></PREDEPT><STD>{recoFlight?.disruptedFlight?.BeginDate.ToString() ?? ""}</STD><STA>{recoFlight?.disruptedFlight?.EndDate.ToString() ?? ""}</STA><PNR></PNR><REASONTEXT>{recoFlight?.dbDistruptedFlight?.DisruptionCode??""}</REASONTEXT><HOSTEDURL></HOSTEDURL><QUEUECODE></QUEUECODE><SUBQUEUECODE></SUBQUEUECODE><TITLE></TITLE><FIRSTNAME></FIRSTNAME><LASTNAME></LASTNAME><HOMEPHONE></HOMEPHONE><WORKPHONE></WORKPHONE><OTHERPHONE></OTHERPHONE><PRIORITYID>1</PRIORITYID><ID>1</ID><SID>1</SID><APPTEMPLATEID></APPTEMPLATEID><REQUESTID></REQUESTID><ETD>{recoFlight?.dbDistruptedFlight?.ETD.ToString() ?? ""}</ETD><ETA>{recoFlight?.dbDistruptedFlight?.ETA.ToString() ?? ""}</ETA></EMAIL></ROOT>";

            return emailXml;

        }
        public string generateEmailTemplateForApprovalAsync(RecoFlight? recoFlight)
        {
            try
            {
                //string serverPath = @"Configs\FlightApproval.json";
                string serverPath = @"/app/Configs/FlightApproval.json"; //OCP
               return GenerateEmailXml(recoFlight, serverPath);
            }
            catch (Exception ex)
            {
                _logHelper.LogError(ex.Message);
                return "";
            }
        }
        public string generateEmailTemplateAsync(RecoFlight? recoFlight)
        {
            try
            {
                //string serverPath = @"Configs\Flight_disruption.json";
                string serverPath = @"/app/Configs/Flight_disruption.json"; //OCP
                return GenerateEmailXml(recoFlight, serverPath);

            }
            catch (Exception ex)
            {
                _logHelper.LogError(ex.Message);
                return "";
            }
        }
        public bool sendTheEmailAsync(RecoFlight? recoFlight,string emailTemplate)
        {
            try
            {
                bool emailStatus = false;
                if (recoFlight != null && recoFlight?.ListOfEmailIDs != null && recoFlight?.ListOfEmailIDs.Count() > 0)
                {
                    foreach (var email in recoFlight?.ListOfEmailIDs)
                    {
                        if (!string.IsNullOrEmpty(email))
                        {
                            _logHelper.LogInfo(" ***  Email ID :  **** " + email);
                            emailTemplate = emailTemplate.Replace("[EMAILIDXXX]", email);
                            emailStatus = sendEmail(emailTemplate);
                            emailTemplate = emailTemplate.Replace(email, "[EMAILIDXXX]");
                        }
                    }
                }
                return emailStatus;
            }
            catch (Exception ex) {
                _logHelper.LogError($"{":-sendTheEmailAsync :-"}{"msg:-"}{ex.Message}");
                return  false;
            }
        }
        private bool sendEmail(string emailTemplate)
        {
            try
            {
                bool _status = false;
                System.ServiceModel.EndpointAddress endPointAddressEmailSMS = null;
                string? sEndpointEmailSMS = _uCG_CredentialsModel.UCG_Endpoints;
                endPointAddressEmailSMS = new System.ServiceModel.EndpointAddress(sEndpointEmailSMS);
                System.ServiceModel.BasicHttpBinding bsBinding = new System.ServiceModel.BasicHttpBinding();

                if (sEndpointEmailSMS.Contains("http://") || sEndpointEmailSMS.Contains("https://"))
                {
                    bsBinding.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport;

                    bsBinding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Windows;
                }
                UCGWebServiceClient objClient = new UCGWebServiceClient(bsBinding, endPointAddressEmailSMS);
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var response = objClient.SendEmailRequest(emailTemplate, _uCG_CredentialsModel.UCG_userId, _uCG_CredentialsModel.UCG_password);
                if (response.InnerXml.ToLower().Contains("success"))
                {
                    _status = true;
                }
                _logHelper.LogInfo(" *** Email Send Status:" + _status + " : " + response?.InnerXml ?? "");
                return _status;
            }
            catch(Exception ex)
            {
                _logHelper.LogError($"{":-sendEmail :-"}{"msg:-"}{ex.Message}");
                return false;
            }
        }
        public bool sendEmailByNHub(DisruptedFlightResponse? disruptionFlight, NhbTemplateResponse nhbTemplates, List<string>? ListOfEmailIDs, string TemplateName)
        {
            try
            {
                NhubTemplateJSON nhubTemplateJSON = _notificationTemplateService.fnGenrateTemplateJSON(disruptionFlight, nhbTemplates, ListOfEmailIDs, TemplateName).Result;
                notificationTemplateHUB? notificationTemplateHUB=JsonConvert.DeserializeObject<notificationTemplateHUB>(nhubTemplateJSON.TemplateJSON);
                NotificationHUBResponse notificationHUBResponse = _notificationHUBService.SendTheNotify(notificationTemplateHUB, nhubTemplateJSON.ID).Result;
                if(!string.IsNullOrEmpty(notificationHUBResponse.RequestId))
                {
                    _logHelper.LogInfo("sendEmailByNHub  :"+ notificationHUBResponse.RequestId+" for Flight No:"+ disruptionFlight?.FlightNumber??"");
                    return true;
                }
                return false;   
            }
            catch (Exception ex)
            {
                _logHelper.LogError(ex.Message);
                return false;
            }
        }
    }
}
