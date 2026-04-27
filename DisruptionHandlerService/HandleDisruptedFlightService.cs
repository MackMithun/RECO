using AutoMapper;
using Microsoft.Identity.Web;
using RECO.DistrubtionHandler_MS.DisruptionHandlerService;
using RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.DistrubtionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.IUtilities;
using RECO.DistrubtionHandler_MS.Models.DatabaseModel;
using RECO.DistrubtionHandler_MS.Models.Enum;
using RECO.DistrubtionHandler_MS.Models.RequestModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel;
using RECO.DistrubtionHandler_MS.Models.ResponseModel.NHub;
using RECO.DistrubtionHandler_MS.Models.ResponseModel.TripModel;
using RECO.DistrubtionHandler_MS.UCGHandlerService.Interface;

namespace RECO.DistrubtionHandler_MS.DistrubtionHandlerService
{
    public class DistrupitonHandlerServices : IHandleDisruptedFlightService
    {
        private readonly ITripInfoLegsService _tripInfoLegsService;
        private readonly IDataMsService _dataMsService;
        private readonly IUCGWebServices _iUCGWebServices;
        private readonly INavitaireService _navitaireService;
        private readonly ILogHelper _logHelper;
        private readonly IMapper _mapper;
        private readonly IRulesMSService _rulesMSService;
        private readonly ICOBRosterService _cobRosterService;
        private readonly IConfiguration _configuration;
        public DistrupitonHandlerServices(ITripInfoLegsService tripInfoLegsService, IRulesMSService rulesMSService, IConfiguration configuration, ICOBRosterService cobRosterService,
            IMapper mapper, IDataMsService dataMsService, IUCGWebServices uCGWebServices, INavitaireService navitaireService, ILogHelper logHelper)
        {
            _tripInfoLegsService = tripInfoLegsService;
            _dataMsService = dataMsService;
            _iUCGWebServices = uCGWebServices;
            _navitaireService = navitaireService;
            _logHelper = logHelper;
            _rulesMSService = rulesMSService;  
            _mapper = mapper;   
            _configuration=configuration;
            _cobRosterService = cobRosterService;       
        }
        public async Task<FlightDetails> GetFlightDetails(Models.RequestModel.FlightData flightData, string token)
        {
            try
            {
                FlightDetails flightDetails = new FlightDetails();
                NavitaireFlightRequest disruptionHandlerRequest = _mapper.Map<NavitaireFlightRequest>(flightData);
                TripInfoLegsResponseModel tripInfoLegsResponse = await _tripInfoLegsService.GetManifest(disruptionHandlerRequest, token);
                flightDetails.origin = tripInfoLegsResponse?.Data?[0].Designator?.Origin;
                int length = tripInfoLegsResponse?.Data?.Count() - 1 ?? 0;
                flightDetails.destination = tripInfoLegsResponse?.Data?[length].Designator?.Destination;
                return flightDetails;
            }
            catch (Exception ex)
            {
                _logHelper.LogError(ex.Message);
                throw new InvalidOperationException(Enum_CustomMessage.InvalidFlight.GetDescription());
            }
        }


        private async Task<RecoFlight> ValidateAndGetDisruptedFlightModel(DisruptedFlight disruptedFlight)
        {
            RecoFlight recoFlight = new RecoFlight();
            recoFlight.disruptedFlight = disruptedFlight;
            #region Database and Navitaire hit 
            NavitaireFlightRequest navitaireFlightRequest = _mapper.Map<NavitaireFlightRequest>(disruptedFlight);
            List<DateTime> disruptedFlightSTD = _navitaireService.VerifySTDInNavitaire(navitaireFlightRequest).Result;
            if (disruptedFlightSTD.Count() > 0)
            {
                disruptedFlight.BeginDate = disruptedFlightSTD[0];
                disruptedFlight.EndDate = disruptedFlightSTD[1];
            }
            recoFlight.navitaireFlight = navitaireFlightRequest;
            recoFlight.ENV=_configuration.GetValue<string>("ENV");
            Task<bool> statusPost = _dataMsService.PostHistoryDetails(disruptedFlight);
            Task<Dictionary<string, string>> appParameterTable = _dataMsService.GetAppParameterList();
            Task<DisruptedFlightResponse> CheckdistruptedFlight = _dataMsService.CheckDisruptedFlightExists(recoFlight.disruptedFlight);
            Task<List<string>> ListOfEmailIDs = _rulesMSService.GetStakeHolderListAsync();
            Task<ExceptionFlightResponse?> exceptionFlightList = _dataMsService.GetExceptionList(recoFlight.navitaireFlight);
            Task<List<CrewMember>> crewMember= _cobRosterService.GetCOBRoster(disruptedFlight);
            Task<NhbTemplateResponse> nhbTemplates = _dataMsService.GetNhbTemplate();
            await Task.WhenAll(statusPost, appParameterTable, CheckdistruptedFlight, ListOfEmailIDs, exceptionFlightList, crewMember);
            recoFlight.ListOfEmailIDs = ListOfEmailIDs.Result;
            recoFlight.exceptionFlightList = exceptionFlightList.Result;
            recoFlight.appParameterList = appParameterTable.Result;
            recoFlight.nhbTemplateResponses = nhbTemplates.Result;
            DisruptedFlightResponse distruptedFlightDB = CheckdistruptedFlight.Result;
            #endregion
            bool _status =await _dataMsService.AddCrewMembers(crewMember.Result);
            if (distruptedFlightDB.FLTID != 0)
            {
                recoFlight =await distruptedFlightExist(recoFlight, distruptedFlightDB, disruptedFlight);
            }
            else
            {
                recoFlight = await distruptedFlightNotExist(recoFlight, distruptedFlightDB, disruptedFlight);
            }
            return recoFlight;
        }

        private async Task<RecoFlight> distruptedFlightExist(RecoFlight recoFlight, DisruptedFlightResponse distruptedFlightDB, DisruptedFlight disruptedFlight)
        {
            recoFlight.dbDistruptedFlight = distruptedFlightDB;
            recoFlight.dbDistruptedFlight = VerifyStatusInNavitaire(recoFlight.navitaireFlight, recoFlight.appParameterList, recoFlight.dbDistruptedFlight, recoFlight).Result;
            if (recoFlight.dbDistruptedFlight.ReasonForNotProcessing == Enum_RECOStatus.WaitingForApproval.ToString())
            {
                return CheckNavitaireBusinessRule(recoFlight);
            }
            else if (recoFlight.dbDistruptedFlight.DisruptionType == Enum_DisruptionType.FLtNotUpdatedinNav.ToString())
            {
                if (recoFlight.dbDistruptedFlight.ETD != null)
                {
                    recoFlight.ReasonNotReaccommodation = Enum_CustomMessage.Reaccommodationcriteria.GetDescription();
                }
                else
                {
                    recoFlight.ReasonNotReaccommodation = Enum_CustomMessage.FLtNotUpdatedinNav.GetDescription();
                }
                recoFlight.dbDistruptedFlight.ReasonForNotProcessing = Enum_ReasonForNotProcessing.FLtNotUpdatedinNav.ToString();
                recoFlight.dbDistruptedFlight.RECOStatus = Enum_RECOStatus.NotProcessed.ToString();
                recoFlight.isProceed = false;
                distruptedFlightDB.DisruptionCode = disruptedFlight.DisruptionCode;
                distruptedFlightDB.IsDelayProcess = true;
                _logHelper.LogInfo($":- {"Start the Email Flight No"} :- {recoFlight?.navitaireFlight?.Identifier}:- {Enum_DisruptionType.FLtNotUpdatedinNav.ToString()}");
                string ENV = _configuration.GetValue<string>("TestKey");
                if (ENV != "This is prod environment.")
                {
                    string MessagePathway = recoFlight.appParameterList[Enum_MessagePathway.MessagePathway.ToString()].ToLower().Trim();
                    if (MessagePathway == Enum_MessagePathway.NhubMode.ToString().ToLower().Trim())
                    {
                        _iUCGWebServices.sendEmailByNHub(distruptedFlightDB, recoFlight.nhbTemplateResponses, recoFlight.ListOfEmailIDs, Enum_Template.Reaccommodation_Not_started.ToString());
                    }
                    else if (MessagePathway == Enum_MessagePathway.UcgMode.ToString().ToLower().Trim())
                    {
                        string emailTemplate = _iUCGWebServices.generateEmailTemplateForApprovalAsync(recoFlight);
                        _iUCGWebServices.sendTheEmailAsync(recoFlight,emailTemplate);
                    }
                }
            }
            else
            {
                if (disruptedFlight?.Source?.ToUpper() == Enum_Source.KAFKA.ToString())
                {
                    if (distruptedFlightDB.DisruptionType == Enum_DisruptionType.cancelled.ToString())
                    {
                        ValidateCancelledEvent(recoFlight, distruptedFlightDB);
                    }
                    if (distruptedFlightDB.DisruptionType == Enum_DisruptionType.delayed.ToString() || distruptedFlightDB.DisruptionType == Enum_DisruptionType.advanced.ToString())
                    {
                        ValidateDelayedorAdvancedEvent(recoFlight, distruptedFlightDB);
                    }
                }
                distruptedFlightDB.CreatedBy = disruptedFlight.CreatedBy;
                distruptedFlightDB.DisruptionCode = disruptedFlight.DisruptionCode;
                distruptedFlightDB.IsDelayProcess = true;
                recoFlight.dbDistruptedFlight = distruptedFlightDB;
                // Check condition
                recoFlight.isProceed = true;
            }
            return recoFlight;
        }
        private async Task<RecoFlight> distruptedFlightNotExist(RecoFlight recoFlight, DisruptedFlightResponse distruptedFlightDB, DisruptedFlight disruptedFlight)
        {
            distruptedFlightDB.Source = disruptedFlight.Source.ToLower().Trim();
            distruptedFlightDB.AirlineCode = disruptedFlight.CarrierCode;
            distruptedFlightDB.FlightNumber = disruptedFlight.Identifier;
            distruptedFlightDB.FlightDate = disruptedFlight.BeginDate;
            distruptedFlightDB.Origin = disruptedFlight.OriginStations[0];
            distruptedFlightDB.Destination = disruptedFlight.DestinationStations[0];
            distruptedFlightDB.STD = disruptedFlight.BeginDate;
            distruptedFlightDB.STA = disruptedFlight.EndDate;
            distruptedFlightDB.CreatedBy = disruptedFlight.CreatedBy == string.Empty || disruptedFlight.CreatedBy == null ? Enum_Source.KAFKA.ToString() : disruptedFlight.CreatedBy;
            distruptedFlightDB.DisruptionType = disruptedFlight.EventType;
            distruptedFlightDB.DisruptionCode = disruptedFlight.DisruptionCode;
            distruptedFlightDB.IsDelayProcess = true;
            recoFlight.dbDistruptedFlight = distruptedFlightDB;
            recoFlight.isProceed = true;
            return recoFlight;  
        }
        private  void ValidateDelayedorAdvancedEvent(RecoFlight recoFlight, DisruptedFlightResponse distruptedFlightDB)
        {
            distruptedFlightDB.DisruptionType = recoFlight.dbDistruptedFlight.DisruptionType;
            if (!(distruptedFlightDB.DisruptionType == Enum_DisruptionType.cancelled.ToString() || distruptedFlightDB.RECOStatus != Enum_RECOStatus.InProgress.ToString()))
            {
                recoFlight.ReasonNotReaccommodation = Enum_CustomMessage.DisruptedFlightMessage.FormatMessage(distruptedFlightDB.FlightNumber, distruptedFlightDB.STD);
                recoFlight.dbDistruptedFlight.RECOStatus = Enum_RECOStatus.NotProcessed.ToString();
                recoFlight.isProceed = false;
                _logHelper.LogInfo($":- {"Start the Email Flight No"} :- {recoFlight?.navitaireFlight?.Identifier}:-{recoFlight?.ReasonNotReaccommodation??""}");
                string ENV = _configuration.GetValue<string>("TestKey");
                if (ENV != "This is prod environment.")
                {
                    string MessagePathway = recoFlight.appParameterList[Enum_MessagePathway.MessagePathway.ToString()].ToLower().Trim();
                    if (MessagePathway == Enum_MessagePathway.NhubMode.ToString().ToLower().Trim())
                    {
                        _iUCGWebServices.sendEmailByNHub(distruptedFlightDB, recoFlight.nhbTemplateResponses, recoFlight.ListOfEmailIDs, Enum_Template.Reaccommodation_Not_started.ToString());
                    }
                    else if (MessagePathway == Enum_MessagePathway.UcgMode.ToString().ToLower().Trim())
                    {
                        string emailTemplate = _iUCGWebServices.generateEmailTemplateForApprovalAsync(recoFlight);
                        _iUCGWebServices.sendTheEmailAsync(recoFlight,emailTemplate);
                    }
                }
                throw new InvalidOperationException("The Disrupted Flight Number :" + distruptedFlightDB.FlightNumber + " already in action of the specified date :" + distruptedFlightDB.STD);
            }
        }

        private void ValidateCancelledEvent(RecoFlight recoFlight, DisruptedFlightResponse distruptedFlightDB)
        {
            distruptedFlightDB.DisruptionType = recoFlight.dbDistruptedFlight.DisruptionType;
            if (distruptedFlightDB.DisruptionType == Enum_DisruptionType.delayed.ToString() || distruptedFlightDB.DisruptionType == Enum_DisruptionType.advanced.ToString())
            {
                recoFlight.ReasonNotReaccommodation = Enum_CustomMessage.DisruptedFlightMessage.FormatMessage(distruptedFlightDB.FlightNumber, distruptedFlightDB.STD);
                recoFlight.dbDistruptedFlight.RECOStatus = Enum_RECOStatus.NotProcessed.ToString();
                recoFlight.isProceed = false;
                _logHelper.LogInfo($":- {"Start the Email Flight No"} :- {recoFlight?.navitaireFlight?.Identifier}:- {recoFlight?.ReasonNotReaccommodation??""}");
                string ENV = _configuration.GetValue<string>("TestKey");
                if (ENV != "This is prod environment.")
                {
                    string MessagePathway = recoFlight.appParameterList[Enum_MessagePathway.MessagePathway.ToString()].ToLower().Trim();
                    if (MessagePathway == Enum_MessagePathway.NhubMode.ToString().ToLower().Trim())
                    {
                        _iUCGWebServices.sendEmailByNHub(distruptedFlightDB, recoFlight.nhbTemplateResponses, recoFlight.ListOfEmailIDs, Enum_Template.Reaccommodation_Not_started.ToString());
                    }
                    else if (MessagePathway == Enum_MessagePathway.UcgMode.ToString().ToLower().Trim())
                    {
                        string emailTemplate = _iUCGWebServices.generateEmailTemplateForApprovalAsync(recoFlight);
                        _iUCGWebServices.sendTheEmailAsync(recoFlight,emailTemplate);
                    }
                }
                throw new InvalidOperationException("The Disrupted Flight Number :" + distruptedFlightDB.FlightNumber + " already in action of the specified date :" + distruptedFlightDB.STD);
            }
            if (!(distruptedFlightDB.RECOStatus == Enum_RECOStatus.NotProcessed.ToString()))
            {
                recoFlight.ReasonNotReaccommodation = Enum_CustomMessage.DisruptedFlightMessage.FormatMessage(distruptedFlightDB.FlightNumber, distruptedFlightDB.STD);
                recoFlight.isProceed = false;
                _logHelper.LogInfo($":- {"Start the Email Flight No"} :- {recoFlight?.navitaireFlight?.Identifier}:- {recoFlight?.ReasonNotReaccommodation??""}");
                string ENV = _configuration.GetValue<string>("TestKey");
                if (ENV != "This is prod environment.")
                {
                    string MessagePathway = recoFlight.appParameterList[Enum_MessagePathway.MessagePathway.ToString()].ToLower().Trim();
                    if (MessagePathway == Enum_MessagePathway.NhubMode.ToString().ToLower().Trim())
                    {
                        _iUCGWebServices.sendEmailByNHub(distruptedFlightDB, recoFlight.nhbTemplateResponses, recoFlight.ListOfEmailIDs, Enum_Template.Reaccommodation_Not_started.ToString());
                    }
                    else if (MessagePathway == Enum_MessagePathway.UcgMode.ToString().ToLower().Trim())
                    {
                        string emailTemplate = _iUCGWebServices.generateEmailTemplateForApprovalAsync(recoFlight);
                        _iUCGWebServices.sendTheEmailAsync(recoFlight,emailTemplate);
                    }
                }
                throw new InvalidOperationException("The Disrupted Flight Number :" + distruptedFlightDB.FlightNumber + " already in action of the specified date :" + distruptedFlightDB.STD);
            }
        }

        private RecoFlight CheckEligiblityWindow(RecoFlight recoFlight)
        {
            recoFlight.dbDistruptedFlight = CheckOperationalWindow(recoFlight.appParameterList, recoFlight.disruptedFlight, recoFlight.dbDistruptedFlight);
            if (recoFlight.dbDistruptedFlight.RECOStatus == Enum_RECOStatus.NotProcessed.ToString())
            {
                recoFlight.isProceed = false;
            }
            else
            {
                recoFlight.isProceed = true;
            }
            return recoFlight;
        }

        private RecoFlight CheckExceptionflight(RecoFlight recoFlight)
        {
            recoFlight.dbDistruptedFlight = CheckExceptionTable(recoFlight.exceptionFlightList, recoFlight.dbDistruptedFlight);
            if (recoFlight.dbDistruptedFlight.ReasonForNotProcessing == Enum_ReasonForNotProcessing.NoActionRequired.ToString())
            {
                recoFlight.ReasonNotReaccommodation = Enum_CustomMessage.noactionrequired.GetDescription();
                recoFlight.dbDistruptedFlight.RECOStatus = Enum_RECOStatus.NotProcessed.ToString();
                recoFlight.isProceed = false;
                _logHelper.LogInfo($":- {"Start the Email Flight No"} :- {recoFlight?.navitaireFlight?.Identifier}:- {Enum_ReasonForNotProcessing.NoActionRequired.ToString()}");
                string MessagePathway = recoFlight.appParameterList[Enum_MessagePathway.MessagePathway.ToString()].ToLower().Trim();
                if (MessagePathway == Enum_MessagePathway.NhubMode.ToString().ToLower().Trim())
                {
                    _iUCGWebServices.sendEmailByNHub(recoFlight.dbDistruptedFlight, recoFlight.nhbTemplateResponses, recoFlight.ListOfEmailIDs, Enum_Template.Reaccommodation_Not_started.ToString());
                }
                else if (MessagePathway == Enum_MessagePathway.UcgMode.ToString().ToLower().Trim())
                {
                    string emailTemplate = _iUCGWebServices.generateEmailTemplateForApprovalAsync(recoFlight);
                    _iUCGWebServices.sendTheEmailAsync(recoFlight,emailTemplate);
                }
            }
            else
            {
                recoFlight.isProceed = true;
            }
            return recoFlight;
        }

        private RecoFlight CheckNavitaireBusinessRule(RecoFlight recoFlight)
        {
            recoFlight.dbDistruptedFlight = CheckBusinessApproval(recoFlight.appParameterList, recoFlight.disruptedFlight, recoFlight.dbDistruptedFlight);
            if(recoFlight.dbDistruptedFlight.ReasonForNotProcessing == Enum_RECOStatus.WaitingForApproval.ToString())
            {
                recoFlight.ReasonNotReaccommodation = Enum_CustomMessage.approvalisrequired.GetDescription();
                recoFlight.dbDistruptedFlight.RECOStatus = Enum_RECOStatus.NotProcessed.ToString();
                recoFlight.isProceed = false;
                _logHelper.LogInfo($":- {"Start the Email Flight No"} :- {recoFlight?.navitaireFlight?.Identifier}:- {Enum_RECOStatus.WaitingForApproval.ToString()}");
                string MessagePathway = recoFlight.appParameterList[Enum_MessagePathway.MessagePathway.ToString()].ToLower().Trim();
                if (MessagePathway == Enum_MessagePathway.NhubMode.ToString().ToLower().Trim())
                {
                    _iUCGWebServices.sendEmailByNHub(recoFlight.dbDistruptedFlight, recoFlight.nhbTemplateResponses, recoFlight.ListOfEmailIDs, Enum_Template.Reaccommodation_Not_started.ToString());
                }
                else if (MessagePathway == Enum_MessagePathway.UcgMode.ToString().ToLower().Trim())
                {
                    string emailTemplate = _iUCGWebServices.generateEmailTemplateForApprovalAsync(recoFlight);
                    _iUCGWebServices.sendTheEmailAsync(recoFlight,emailTemplate);
                }
            }
            if (recoFlight.dbDistruptedFlight.RECOStatus == Enum_RECOStatus.NotProcessed.ToString())
            {
                recoFlight.isProceed = false;
            }
            else
            {
                recoFlight.isProceed = true;
            }
            return recoFlight;
        }

        private RecoFlight CheckNavitaireFlightStatus(RecoFlight recoFlight)
        {
            if (recoFlight.dbDistruptedFlight.FLTID == 0)
            {
                recoFlight.dbDistruptedFlight = VerifyStatusInNavitaire(recoFlight.navitaireFlight, recoFlight.appParameterList, recoFlight.dbDistruptedFlight, recoFlight).Result;
                if (recoFlight.dbDistruptedFlight.DisruptionType == Enum_DisruptionType.FLtNotUpdatedinNav.ToString())
                {
                    if (recoFlight.dbDistruptedFlight.ETD != null)
                    {
                        recoFlight.ReasonNotReaccommodation =Enum_CustomMessage.Reaccommodationcriteria.GetDescription();
                    }
                    else
                    {
                        recoFlight.ReasonNotReaccommodation = Enum_CustomMessage.FLtNotUpdatedinNav.GetDescription();
                    }
                    recoFlight.dbDistruptedFlight.ReasonForNotProcessing = Enum_ReasonForNotProcessing.FLtNotUpdatedinNav.ToString();
                    recoFlight.dbDistruptedFlight.RECOStatus = Enum_RECOStatus.NotProcessed.ToString();
                    recoFlight.isProceed = false;
                    recoFlight.dbDistruptedFlight.IsDelayProcess = false;
                    _logHelper.LogInfo($":- {"Start the Email Flight No"} :- {recoFlight?.navitaireFlight?.Identifier}:- {Enum_DisruptionType.FLtNotUpdatedinNav.ToString()}");
                    string MessagePathway = recoFlight.appParameterList[Enum_MessagePathway.MessagePathway.ToString()].ToLower().Trim();
                    if (MessagePathway == Enum_MessagePathway.NhubMode.ToString().ToLower().Trim())
                    {
                        _iUCGWebServices.sendEmailByNHub(recoFlight.dbDistruptedFlight, recoFlight.nhbTemplateResponses, recoFlight.ListOfEmailIDs, Enum_Template.Reaccommodation_Not_started.ToString());
                    }
                    else if (MessagePathway == Enum_MessagePathway.UcgMode.ToString().ToLower().Trim())
                    {
                        string emailTemplate = _iUCGWebServices.generateEmailTemplateForApprovalAsync(recoFlight);
                        _iUCGWebServices.sendTheEmailAsync(recoFlight,emailTemplate);
                    }

                }
            }
            return recoFlight;

        }

        private async Task<RecoFlight> SaveDisruptionAndAlert(RecoFlight recoFlight)
        {
            bool _DBStatus=await PostDisruptedFlight(recoFlight.dbDistruptedFlight);
            if (!(recoFlight.dbDistruptedFlight.ReasonForNotProcessing == Enum_ReasonForNotProcessing.InProgress.ToString()))
            {
                throw new InvalidOperationException(recoFlight.dbDistruptedFlight.ReasonForNotProcessing);
            }

            _logHelper.LogInfo($":- {"Start the Email Flight No"} :- {recoFlight?.navitaireFlight?.Identifier??""}");
            SendNotificationEmail(recoFlight);
            if (!string.IsNullOrEmpty(recoFlight.exceptionFlightList.AlternateFlightNumber) && recoFlight.exceptionFlightList.ExceptionType.ToLower().Contains("1"))
            {
                recoFlight.exceptionFlightList.AlternateFlightNumber = recoFlight.exceptionFlightList.AlternateFlightNumber;
            }
            else
            {
                recoFlight.exceptionFlightList.AlternateFlightNumber = "";
            }
            return recoFlight;
        }

        public async Task<RecoFlight> CheckEligibility(DisruptedFlight disruptedFlight)
        {
            try
            {
                RecoFlight recoFlight = ValidateAndGetDisruptedFlightModel(disruptedFlight).Result;
                if (recoFlight.isProceed)
                {
                    recoFlight = CheckEligiblityWindow(recoFlight);
                }

                if (recoFlight.isProceed)
                {
                    recoFlight = CheckExceptionflight(recoFlight);
                }
                if (recoFlight.isProceed)
                {
                    recoFlight = CheckNavitaireFlightStatus(recoFlight);
                }
                if (recoFlight.isProceed)
                {
                    recoFlight = CheckNavitaireBusinessRule(recoFlight);
                }
                recoFlight = await SaveDisruptionAndAlert(recoFlight);
                return recoFlight;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- CheckEligibility"} :- {ex?.Message??""}");
                throw new InvalidOperationException(ex?.InnerException?.InnerException?.Message?? ex?.InnerException?.Message?? ex.Message);
            }
        }

        public async Task<List<DisruptedFlight>> DisruptedFlight()
        {
            try
            {
                List<DisruptedFlight> disruptedFlights = new List<DisruptedFlight>();       
                List<DisruptedFlightResponse> disruptedFlightResponse = await _dataMsService.ListOfDisruptedFlight();
                if (disruptedFlightResponse.Count()>0)
                {
                    List<DisruptedFlightResponse> ListOfRetryFlight = disruptedFlightResponse.Where(
                        x => x.IsDelayProcess == false && x.RECOStatus == Enum_RECOStatus.NotProcessed.ToString()
                        && x.ReasonForNotProcessing == Enum_ReasonForNotProcessing.FLtNotUpdatedinNav.ToString()
                        && (DateTime.UtcNow - x.CreatedOn.ToUniversalTime()).TotalMinutes > 10).OrderBy(x => x.CreatedOn).ToList(); 
                    if (ListOfRetryFlight.Count()>0)
                    {
                        foreach (var RetryFlight in ListOfRetryFlight)
                        {
                            DisruptedFlight obj = new DisruptedFlight
                            {
                                BeginDate = RetryFlight.STD,
                                EndDate = RetryFlight.STA,
                                CarrierCode = RetryFlight.AirlineCode,
                                Identifier = RetryFlight.FlightNumber,
                                FlightType = 1,
                                OriginStations = new List<string> { RetryFlight.Origin },
                                DestinationStations = new List<string> { RetryFlight.Destination },
                                Source = RetryFlight.Source,
                                DisruptionCode = RetryFlight.DisruptionCode,
                                AutoApproval = false,
                                EventType = RetryFlight.DisruptionType,
                                CreatedBy = RetryFlight.CreatedBy,
                            };
                            RetryFlight.IsDelayProcess = true;
                            RetryFlight.Source= RetryFlight.Source+" RECO";
                            UpdateDisruptedFlights updateDisruptedFlights = UpdateDisruptedFlight(RetryFlight);
                            bool _status = await _dataMsService.updatedisruptedFlightDetails(updateDisruptedFlights);
                            disruptedFlights.Add(obj);
                        }
                    }
                }
                return disruptedFlights;    
            }
            catch(Exception ex)
            {
                _logHelper.LogError($"{" :- DisruptedFlight"} :- {ex?.Message??""}");
                return new List<DisruptedFlight>();   
            }
        }
        // ** new changes ** //
        private DisruptedFlightResponse CheckOperationalWindow(Dictionary<string, string> appParameterTable, DisruptedFlight? disruptedFlight, DisruptedFlightResponse disruptedFlightResponse)
        {
            try
            {
                DateTime? _cancelFlightDepartureDateTime = new DateTime?();
                _cancelFlightDepartureDateTime = disruptedFlight.BeginDate;
                DateTime currentDateTimeUCT = DateTime.UtcNow;
                DateTime currentDateTime = TimeZoneService.convertToIND(currentDateTimeUCT);
                _logHelper.LogInfo($"{" :- CheckOperationalWindow"} :- {" Flight No :"}{disruptedFlight.Identifier}:- {"start DateTime :- "}{currentDateTime} :-{" FlightDate :-"}{disruptedFlight.BeginDate}");
                DateTime lowerBound = currentDateTime.AddHours(Convert.ToInt32(appParameterTable[Enum_AppParameters.OPWMin.ToString()]));
                DateTime upperBound = currentDateTime.AddHours(Convert.ToInt32(appParameterTable[Enum_AppParameters.OPWMax.ToString()]));
                if (!(_cancelFlightDepartureDateTime >= lowerBound && _cancelFlightDepartureDateTime <= upperBound))
                {
                    disruptedFlightResponse.ReasonForNotProcessing = Enum_ReasonForNotProcessing.OutsideEligibilityWindow.ToString();
                    disruptedFlightResponse.RECOStatus = Enum_RECOStatus.NotProcessed.ToString();
                }
                else
                {
                    disruptedFlightResponse.ReasonForNotProcessing = Enum_ReasonForNotProcessing.InProgress.ToString();
                    disruptedFlightResponse.RECOStatus = Enum_RECOStatus.InProgress.ToString();
                }
                return disruptedFlightResponse;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- CheckOperationalWindow"} :- {ex?.Message??""}");
                throw new InvalidOperationException(Enum_CustomMessage.WindowValidationFailed.GetDescription());
            }
        }
        //0= NoActionRequired,1= MoveToSpecificFlight, 2=NoMoveOnTo,   move to specific flight
        private DisruptedFlightResponse CheckExceptionTable(ExceptionFlightResponse exceptionFlightList, DisruptedFlightResponse disruptedFlightResponse)
        {
            try
            {

                if (exceptionFlightList != null && exceptionFlightList.ExceptionType != null && exceptionFlightList.ExceptionType.ToLower().Contains("0"))
                {
                    disruptedFlightResponse.ReasonForNotProcessing = Enum_ReasonForNotProcessing.NoActionRequired.ToString();
                    disruptedFlightResponse.RECOStatus = Enum_RECOStatus.NotProcessed.ToString();
                }
                return disruptedFlightResponse;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- CheckExceptionTable"} :- {ex?.Message??""}");
                throw new InvalidOperationException(Enum_CustomMessage.ExceptionTableValidation.GetDescription());
            }

        }
        private DisruptedFlightResponse CheckBusinessApproval(Dictionary<string, string> appParameterTable, DisruptedFlight? disruptedFlight, DisruptedFlightResponse disruptedFlightResponse)
        {
            try
            {
                if (!(bool)disruptedFlight?.AutoApproval)
                {
                    if (appParameterTable[Enum_AppParameters.IsAutoRecommendation.ToString()].ToLower().Trim() == "false")
                    {
                        disruptedFlightResponse.ReasonForNotProcessing = Enum_RECOStatus.WaitingForApproval.ToString();
                        disruptedFlightResponse.RECOStatus = Enum_RECOStatus.NotProcessed.ToString();
                    }
                    if (appParameterTable[Enum_AppParameters.IsAutoRecommendation.ToString()].ToLower().Trim() == "true")
                    {
                        if (disruptedFlightResponse.ReasonForNotProcessing == Enum_ReasonForNotProcessing.OutsideEligibilityWindow.ToString())
                        {
                            disruptedFlightResponse.ReasonForNotProcessing = Enum_ReasonForNotProcessing.OutsideEligibilityWindow.ToString();
                            disruptedFlightResponse.RECOStatus = Enum_RECOStatus.NotProcessed.ToString();
                        }
                        else if (disruptedFlightResponse.ReasonForNotProcessing == Enum_ReasonForNotProcessing.NoActionRequired.ToString())
                        {
                            disruptedFlightResponse.ReasonForNotProcessing = Enum_ReasonForNotProcessing.NoActionRequired.ToString();
                            disruptedFlightResponse.RECOStatus = Enum_RECOStatus.NotProcessed.ToString();
                        }
                        else
                        {
                            disruptedFlightResponse.ReasonForNotProcessing = Enum_ReasonForNotProcessing.InProgress.ToString();
                            disruptedFlightResponse.RECOStatus = Enum_RECOStatus.InProgress.ToString();
                        }
                    }
                }
                else
                {
                    StatusUpdate_AutoApproval(disruptedFlightResponse);
                }
                return disruptedFlightResponse;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- CheckBusinessApproval"} :- {ex?.Message ?? ""}");
                throw new InvalidOperationException(Enum_CustomMessage.BusinessApprovalValidation.GetDescription());
            }
        }

        private static void StatusUpdate_AutoApproval(DisruptedFlightResponse disruptedFlightResponse)
        {
            if (disruptedFlightResponse.ReasonForNotProcessing == Enum_ReasonForNotProcessing.OutsideEligibilityWindow.ToString())
            {
                disruptedFlightResponse.ReasonForNotProcessing = Enum_ReasonForNotProcessing.OutsideEligibilityWindow.ToString();
                disruptedFlightResponse.RECOStatus = Enum_RECOStatus.NotProcessed.ToString();
            }
            else if (disruptedFlightResponse.ReasonForNotProcessing == Enum_ReasonForNotProcessing.NoActionRequired.ToString())
            {
                disruptedFlightResponse.ReasonForNotProcessing = Enum_ReasonForNotProcessing.NoActionRequired.ToString();
                disruptedFlightResponse.RECOStatus = Enum_RECOStatus.NotProcessed.ToString();
            }
            else
            {
                disruptedFlightResponse.ReasonForNotProcessing = Enum_ReasonForNotProcessing.InProgress.ToString();
                disruptedFlightResponse.RECOStatus = Enum_RECOStatus.InProgress.ToString();
            }
        }

        private async Task<DisruptedFlightResponse> VerifyStatusInNavitaire(NavitaireFlightRequest navitaireFlightRequest, Dictionary<string, string> appParameterTable, DisruptedFlightResponse disruptedFlightResponse, RecoFlight recoFlight = null)
        {
            try
            {
                ListOfHandleDiscruption listOfHandle = new ListOfHandleDiscruption();
                listOfHandle = await _navitaireService.GetNavitaireFlight(navitaireFlightRequest, appParameterTable);
                disruptedFlightResponse.DisruptionType = listOfHandle._DisruptionType.ToString();
                disruptedFlightResponse.ETD= listOfHandle?.legKeyStatusFlightInformationModel?.EstimateDepartureTime;
                disruptedFlightResponse.ETA = listOfHandle?.legKeyStatusFlightInformationModel?.EstimateArrivalTime;
                return disruptedFlightResponse;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- VerifyStatusInNavitaire"} :- {ex?.Message ?? ""}");
                throw new InvalidOperationException(Enum_CustomMessage.NavitaireValidation.GetDescription());
            }
        }
        private void SendNotificationEmail(RecoFlight? recoFlight)
        {
            try
            {
                string MessagePathway = recoFlight.appParameterList[Enum_MessagePathway.MessagePathway.ToString()].ToLower().Trim();
                if (MessagePathway == Enum_MessagePathway.NhubMode.ToString().ToLower().Trim())
                {
                    _iUCGWebServices.sendEmailByNHub(recoFlight.dbDistruptedFlight, recoFlight.nhbTemplateResponses, recoFlight.ListOfEmailIDs, Enum_Template.Reaccommodation_start.ToString());
                }
                else if (MessagePathway == Enum_MessagePathway.UcgMode.ToString().ToLower().Trim())
                {
                    string emailTemplate = _iUCGWebServices.generateEmailTemplateAsync(recoFlight);
                    _iUCGWebServices.sendTheEmailAsync(recoFlight,emailTemplate);
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- SendNotificationEmail"} :- {ex?.Message ?? ""}");
                throw new InvalidOperationException(Enum_CustomMessage.NotificationValidation.GetDescription());
            }
        }
        private async Task<bool> PostDisruptedFlight(DisruptedFlightResponse addDisruptedFlightsModelResponse)
        {
            try
            {
                bool _status = false;
                if (addDisruptedFlightsModelResponse?.FLTID == null || addDisruptedFlightsModelResponse?.FLTID == 0)
                {
                    //Add data
                    DisruptedFlightsResponse postDisruptedFlightsModel = AddDisruptedFlight(addDisruptedFlightsModelResponse);
                    _status = await _dataMsService.PostdisruptedFlightDetails(postDisruptedFlightsModel);
                }
                else
                {
                    // update data
                    UpdateDisruptedFlights updateDisruptedFlights = UpdateDisruptedFlight(addDisruptedFlightsModelResponse);
                    _status = await _dataMsService.updatedisruptedFlightDetails(updateDisruptedFlights);
                }
                return _status;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- PostDisruptedFlight"} :- {ex?.Message ?? ""}");
                return false;
            }
        }
        private DisruptedFlightsResponse AddDisruptedFlight(DisruptedFlightResponse disruptedFlightResponse)
        {
            try
            {
                if (disruptedFlightResponse.RECOStatus == Enum_RECOStatus.InProgress.ToString())
                {
                    disruptedFlightResponse.IsApproved = true;
                }
                else
                {
                    disruptedFlightResponse.IsApproved = false;
                }
                DisruptedFlightsResponse addDisruptedFlightsModel = new DisruptedFlightsResponse
                {
                    AirlineCode = disruptedFlightResponse.AirlineCode,
                    FlightNumber = disruptedFlightResponse.FlightNumber,
                    FlightDate = disruptedFlightResponse.FlightDate,
                    Origin = disruptedFlightResponse.Origin,
                    Destination = disruptedFlightResponse.Destination,
                    STD = disruptedFlightResponse.STD,
                    ETD = disruptedFlightResponse.ETD,
                    STA = disruptedFlightResponse.STA,
                    ETA = disruptedFlightResponse.ETA,
                    DisruptionType = disruptedFlightResponse.DisruptionType,
                    RECOStatus = disruptedFlightResponse.RECOStatus,
                    IsApproved = disruptedFlightResponse.IsApproved,
                    Retry = 1,
                    Source = disruptedFlightResponse.Source == Enum_Source.Manual.ToString() ? Enum_Source.Manual.ToString() : Enum_Source.KAFKA.ToString(),
                    ReasonForNotProcessing = disruptedFlightResponse.ReasonForNotProcessing,
                    ReaccommodatedPNRCount = 0,
                    NotReaccommodatedPNRCount = 0,
                    DisruptionCode= disruptedFlightResponse.DisruptionCode, 
                    CreatedBy= disruptedFlightResponse.CreatedBy,
                    IsDelayProcess = disruptedFlightResponse.IsDelayProcess,
                };
                return addDisruptedFlightsModel;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- AddDisruptedFlight"} :- {ex?.Message ?? ""}");
                throw new InvalidOperationException(Enum_CustomMessage.DisruptedFlightValidation.ToString());
            }
        }
        private UpdateDisruptedFlights UpdateDisruptedFlight(DisruptedFlightResponse disruptedFlightResponse)
        {
            try
            {
                if (disruptedFlightResponse.RECOStatus == Enum_RECOStatus.InProgress.ToString())
                {
                    disruptedFlightResponse.IsApproved = true;
                }
                else
                {
                    disruptedFlightResponse.IsApproved = false;
                }
                UpdateDisruptedFlights updateDisruptedFlights = new UpdateDisruptedFlights
                {
                    FLTID = disruptedFlightResponse.FLTID,
                    AirlineCode = disruptedFlightResponse.AirlineCode,
                    FlightNumber = disruptedFlightResponse.FlightNumber,
                    FlightDate = disruptedFlightResponse.FlightDate,
                    Origin = disruptedFlightResponse.Origin,
                    Destination = disruptedFlightResponse.Destination,
                    STD = disruptedFlightResponse.STD,
                    ETD = disruptedFlightResponse.ETD,
                    STA = disruptedFlightResponse.STA,
                    ETA = disruptedFlightResponse.ETA,
                    DisruptionType = disruptedFlightResponse.DisruptionType,
                    RECOStatus = disruptedFlightResponse.RECOStatus,
                    IsApproved = disruptedFlightResponse.IsApproved,
                    Retry = disruptedFlightResponse.Retry + 1,
                    Source = disruptedFlightResponse.Source == Enum_Source.Manual.ToString() ? Enum_Source.Manual.ToString() : Enum_Source.KAFKA.ToString(),
                    ReasonForNotProcessing = disruptedFlightResponse.ReasonForNotProcessing.ToString(),
                    ReaccommodatedPNRCount = disruptedFlightResponse.ReaccommodatedPNRCount,
                    NotReaccommodatedPNRCount = disruptedFlightResponse.NotReaccommodatedPNRCount,
                    DisruptionCode = disruptedFlightResponse.DisruptionCode,
                    ModifiedBy = disruptedFlightResponse.CreatedBy, 
                    IsDelayProcess= disruptedFlightResponse.IsDelayProcess,     
                };
                return updateDisruptedFlights;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{" :- UpdateDisruptedFlight"} :- {ex?.Message ?? ""}");
                throw new InvalidOperationException(Enum_CustomMessage.DisruptedFlightValidation.GetDescription());
            }
        }
        // ** new changes **/ /    
    }
}