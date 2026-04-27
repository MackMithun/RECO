using Azure;
using Newtonsoft.Json;
using RECO.DistrubtionHandler_MS.IUtilities;
using RECO.DistrubtionHandler_MS.Models.DatabaseModel;
using RECO.DistrubtionHandler_MS.Models.Enum;
using RECO.DistrubtionHandler_MS.Models.ResponseModel.NHub;
using RECO.DistrubtionHandler_MS.UCGHandlerService.Interface;

namespace RECO.DistrubtionHandler_MS.UCGHandlerService
{
    public class NotificationTemplateService : INotificationTemplateService
    {
        private readonly ILogHelper _logHelper;
        private readonly string _caller = typeof(NotificationTemplateService).Name;
        private readonly IConfiguration _configuration;
        public NotificationTemplateService(ILogHelper logHelper, IConfiguration configuration)
        {
            _logHelper = logHelper;
            _configuration = configuration;
        }
        public async Task<NhubTemplateJSON> fnGenrateTemplateJSON(DisruptedFlightResponse? disruptionFlight, NhbTemplateResponse nhbTemplates, List<string>? ListOfEmailIDs, string TemplateName)
        {

            try
            {
                if (TemplateName == Enum_Template.Reaccommodation_start.ToString())
                {
                    TemplateResponse? nhbTemplate = nhbTemplates.data?.FirstOrDefault(y =>y.RecoTemplateName.ToLower().Trim() == TemplateName.ToLower().Trim()); 
                    if (nhbTemplate !=null  && nhbTemplate.Variables.Count() > 0)
                    {
                        List<variable> variables = new List<variable>();
                        List<nhbvariable> nhubvariable  = nhbTemplate?.Variables?.ToList() ?? new List<nhbvariable>();
                        if(nhubvariable.Count()>0)
                        {
                            variables.Add(addVariable(nhubvariable[0].NhbID, nhubvariable[0].Name, disruptionFlight?.ETA.HasValue == true ? disruptionFlight.ETA.Value.ToString("dd-MM-yyyy HH:mm:ss") : " "));
                            variables.Add(addVariable(nhubvariable[1].NhbID, nhubvariable[1].Name, disruptionFlight?.Source ?? " "));
                            variables.Add(addVariable(nhubvariable[2].NhbID, nhubvariable[2].Name, disruptionFlight?.CreatedBy ?? " "));
                            variables.Add(addVariable(nhubvariable[3].NhbID, nhubvariable[3].Name, disruptionFlight?.DisruptionType ?? "  "));
                            variables.Add(addVariable(nhubvariable[4].NhbID, nhubvariable[4].Name, disruptionFlight?.STA.HasValue == true ? disruptionFlight.STA.Value.ToString("dd-MM-yyyy HH:mm:ss") : " "));
                            variables.Add(addVariable(nhubvariable[5].NhbID, nhubvariable[5].Name, disruptionFlight?.STD.HasValue == true ? disruptionFlight.STD.Value.ToString("dd-MM-yyyy HH:mm:ss") : " "));
                            variables.Add(addVariable(nhubvariable[6].NhbID, nhubvariable[6].Name, disruptionFlight?.Destination ?? "  "));
                            variables.Add(addVariable(nhubvariable[7].NhbID, nhubvariable[7].Name, disruptionFlight?.Origin ?? "  "));
                            variables.Add(addVariable(nhubvariable[8].NhbID, nhubvariable[8].Name, disruptionFlight?.FlightNumber ?? " "));
                            variables.Add(addVariable(nhubvariable[9].NhbID, nhubvariable[9].Name, disruptionFlight?.ETD.HasValue == true ? disruptionFlight.ETD.Value.ToString("dd-MM-yyyy HH:mm:ss") : " "));

                            return new NhubTemplateJSON
                            {
                                ID = nhbTemplate.TemplateID,
                                TemplateJSON = fnTemplateJSON(variables, nhbTemplate, ListOfEmailIDs),
                            };
                        }
                    }
                    
                }
                else if (TemplateName == Enum_Template.Reaccommodation_Not_started.ToString())
                {
                    TemplateResponse? nhbTemplate = nhbTemplates.data?.FirstOrDefault(y => y.RecoTemplateName.ToLower().Trim() == TemplateName.ToLower().Trim());
                    if (nhbTemplate != null && nhbTemplate.Variables != null && nhbTemplate.Variables.Count() > 0)
                    {
                        List<nhbvariable> nhubvariable = nhbTemplate?.Variables?.ToList() ?? new List<nhbvariable>();
                        if (nhubvariable.Count() > 0)
                        {
                            List<variable> variables = new List<variable>();
                            variables.Add(addVariable(nhubvariable[0].NhbID, nhubvariable[0].Name,disruptionFlight?.ETA.HasValue == true ? disruptionFlight.ETA.Value.ToString("dd-MM-yyyy HH:mm:ss") : " "));
                            variables.Add(addVariable(nhubvariable[1].NhbID, nhubvariable[1].Name,disruptionFlight?.ReasonForNotProcessing ?? "  "));
                            variables.Add(addVariable(nhubvariable[2].NhbID, nhubvariable[2].Name,disruptionFlight?.CreatedBy ?? "  "));
                            variables.Add(addVariable(nhubvariable[3].NhbID, nhubvariable[3].Name,disruptionFlight?.Source ?? "  "));
                            variables.Add(addVariable(nhubvariable[4].NhbID, nhubvariable[4].Name,disruptionFlight?.DisruptionType ?? "  "));
                            variables.Add(addVariable(nhubvariable[5].NhbID, nhubvariable[5].Name,disruptionFlight?.STA.HasValue == true ? disruptionFlight.STA.Value.ToString("dd-MM-yyyy HH:mm:ss") : "  "));
                            variables.Add(addVariable(nhubvariable[6].NhbID, nhubvariable[6].Name,disruptionFlight?.STD.HasValue == true ? disruptionFlight.STD.Value.ToString("dd-MM-yyyy HH:mm:ss") : "  "));
                            variables.Add(addVariable(nhubvariable[7].NhbID, nhubvariable[7].Name,disruptionFlight?.Destination ?? "  "));
                            variables.Add(addVariable(nhubvariable[8].NhbID, nhubvariable[8].Name,disruptionFlight?.Origin ?? "  "));
                            variables.Add(addVariable(nhubvariable[9].NhbID, nhubvariable[9].Name,disruptionFlight?.FlightNumber ?? "  "));
                            variables.Add(addVariable(nhubvariable[10].NhbID, nhubvariable[10].Name, disruptionFlight?.ETD.HasValue == true ? disruptionFlight.ETD.Value.ToString("dd-MM-yyyy HH:mm:ss") : "  "));

                            return new NhubTemplateJSON
                            {
                                ID = nhbTemplate.TemplateID,
                                TemplateJSON = fnTemplateJSON(variables, nhbTemplate, ListOfEmailIDs),
                            };
                        }
                    }
                }
                return new NhubTemplateJSON
                {
                    ID = 205,
                    TemplateJSON = "",
                };
            }
            catch (Exception ex)
            {
                _logHelper.LogConsoleException(ex);
                return new NhubTemplateJSON
                {
                    ID = 205,
                    TemplateJSON = "",
                };
            }
        }
        private variable addVariable(int id, string name, string value)
        {
            return new variable
            {
                id = id,
                name = name,
                value = value
            };
        }
        private channel addChannel(int id, string name)
        {
            return new channel
            {
                id = id,
                name = name,
            };
        }
        public string fnTemplateJSON(List<variable> variables, TemplateResponse templateResponse, List<string>? stackholders)
        {
            List<channel> channels = new List<channel>();
            channels.Add(addChannel(4, "Email"));
            notificationTemplateHUB? notificationTemplateHUB = new notificationTemplateHUB();
            notificationTemplateHUB.name = templateResponse.NhubTemplateName;
            notificationTemplateHUB.language = templateResponse.language;
            notificationTemplateHUB.priority = templateResponse.priority;
            notificationTemplateHUB.category = templateResponse.category;
            notificationTemplateHUB.businessGroup = templateResponse.businessGroup;
            notificationTemplateHUB.app.id = templateResponse.appid;
            notificationTemplateHUB.app.name = templateResponse.appname;
            notificationTemplateHUB.channels.AddRange(channels);
            notificationTemplateHUB.variables.AddRange(variables);
            notificationTemplateHUB.enrichmentRequired = false;
            if(stackholders?.Count()>0)
            {
                foreach (var email in stackholders) {
                    if (!string.IsNullOrEmpty(email))
                    {
                        notificationTemplateHUB.recipients.emailRecipient.to.Add(email);
                    }
                }

            }
            else
            {
                notificationTemplateHUB.recipients.emailRecipient.to.Add(_configuration.GetValue<string>("emailRecipientTo"));
                notificationTemplateHUB.recipients.emailRecipient.cc.Add(_configuration.GetValue<string>("emailRecipientCC"));
            }
            return JsonConvert.SerializeObject(notificationTemplateHUB);
        }

    }
}
