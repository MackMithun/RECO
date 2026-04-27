using AutoMapper;
using Newtonsoft.Json;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.Models.DatabaseModel;
using RECO.Reaccommodation_MS.Models.Enum;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.LegModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.ManifestModel;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;
using RECO.Reaccommodation_MS.UCGHandlerService.Interface;
using System.Collections.Concurrent;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class SpecificFlightService : ISpecificFlightService
    {
        #region Private Properties and Variables
        private readonly IPNRHandlerService _pnrHandlerService;
        private readonly IReaccommodationHandlerService _reaccommodationService;
        private readonly ILogHelper _logHelper;
        private readonly IDataMsService _dataMsService;
        private readonly IRulesMSService _rulesMSService;
        private readonly IPNR_HandlerService _pNR_HandlerService;
        private readonly IUCGWebServices _uCGWebService;
        private readonly INavitaireAuthorizationService _authorizationService;
        private readonly IMapper _mapper;
        private static bool _isProcessingQueue = false;
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Allows only one task at a time
        private static ConcurrentQueue<Func<Task>> _taskQueue = new ConcurrentQueue<Func<Task>>();
        #endregion

        #region Constructor
        public SpecificFlightService(IPNRHandlerService pnrHandlerService, IRulesMSService rulesMSService,
            IUCGWebServices iUCGWebServices, IReaccommodationHandlerService reaccommodationService, ILogHelper logHelper, IDataMsService dataMsService
            , IPNR_HandlerService pNR_HandlerService, IMapper mapper, INavitaireAuthorizationService authorizationService)
        {
            _reaccommodationService = reaccommodationService;
            _logHelper = logHelper;
            _pnrHandlerService = pnrHandlerService;
            _dataMsService = dataMsService;
            _rulesMSService = rulesMSService;
            _pNR_HandlerService = pNR_HandlerService;
            _mapper = mapper;
            _uCGWebService = iUCGWebServices;
            _authorizationService = authorizationService;
        }
        #endregion

        public async Task ReaccommodateToSpecificFlight(DisruptedFlight disruptedFlight, string Token)
        {
            Reaccommodation_Model reaccommodationms = new Reaccommodation_Model();
            try
            {
                await _logHelper.LogInfo(":- ReaccommodateToSpecificFlight start -: ");
                reaccommodationms.disruptedFlightDB = await _dataMsService.GetDisruptedFlight(disruptedFlight);
                if (reaccommodationms.disruptedFlightDB == null || reaccommodationms.disruptedFlightDB?.FLTID == 0)
                {
                    throw new InvalidOperationException("DisruptedFlight Details Not Found in Database for Flight number =" + disruptedFlight.oldIdentifier);
                }
                reaccommodationms.PNRDetailList = new List<PNRDetail>();
                reaccommodationms.TripInfoLegs = _mapper.Map<TripInfoLegsRequest>(disruptedFlight);
                reaccommodationms.DisruptedFlight = _mapper.Map<DisruptedFlightRequest>(disruptedFlight);
                reaccommodationms.SortedPNRList = _pnrHandlerService?.GetUnSortedPNR(reaccommodationms, Token)?.Result?._listOfPNR;
                reaccommodationms.AppParameterList =await _dataMsService.GetAppParameterList();
                reaccommodationms.nhbTemplate = await _dataMsService.GetNhbTemplate();
                if (reaccommodationms?.SortedPNRList?.Count > 0)
                {
                    #region AddPNRDetails
                    bool _saveStatus = await _dataMsService.AddPNRDetails(reaccommodationms.SortedPNRList);
                    if (_saveStatus)
                    {
                        reaccommodationms.PNRDetailList = await _dataMsService.GetPNRDetails(reaccommodationms.disruptedFlightDB);
                    }
                    if (reaccommodationms.PNRDetailList.Count == 0 || reaccommodationms.PNRDetailList == null)
                    {
                        throw new InvalidOperationException("PNR Details not found in database for Flight number =" + reaccommodationms?.disruptedFlightDB?.FlightNumber);
                    }
                    reaccommodationms.ListofRestrictedSSR = await _rulesMSService.GetRestrictedSSRList();
                    #endregion
                    reaccommodationms.DisruptedFlight.identifier = disruptedFlight.newIdentifier;
                    reaccommodationms.ManifestDetail = await _pnrHandlerService.GetManifestDetails(reaccommodationms.DisruptedFlight, Token);

                    string? _Token = await _authorizationService.GetTokenAsync();
                    if (!string.IsNullOrEmpty(_Token))
                    {
                        await Pro_ReaccommodateToSpecific(reaccommodationms, _Token);
                    }
                    else
                    {
                        await _logHelper.LogInfo(":- ReaccommodateToSpecificFlight Reco Token not created :-");
                        throw new InvalidOperationException("Reco Token not created");
                    }

                }
                else
                {
                    await NoPNRAvailable(reaccommodationms?.disruptedFlightDB);
                }
            }
            catch (Exception ex)
            {
                await CatchException(reaccommodationms?.disruptedFlightDB, ex, reaccommodationms?.AppParameterList, reaccommodationms.nhbTemplate);
            }
        }

        private async Task Pro_ReaccommodateToSpecific(Reaccommodation_Model reaccommodationms, string _Token)
        {
            try
            {
                var pnrDetails = new PNRDetails
                {
                    disruptedFlightDB = reaccommodationms.disruptedFlightDB,
                    pnrDetail = new List<PNRDetail>(),
                    routingDetail = new List<RoutingDetail>()
                };
                if (reaccommodationms.ManifestDetail != null && reaccommodationms.ManifestDetail.Data != null && reaccommodationms.ManifestDetail.Data.Any())
                {
                    pnrDetails = await _reaccommodationService.ReaccomodateToSpecificFlight(reaccommodationms, _Token);
                }
                else
                {
                    await _logHelper.LogInfo(":- Pro_ReaccommodateToSpecific FlightNo :- " + reaccommodationms?.disruptedFlightDB?.FlightNumber ?? "");
                }
                _taskQueue.Enqueue(async () => await ProcessSaveDataAndNotify(pnrDetails, reaccommodationms?.AppParameterList, reaccommodationms?.nhbTemplate));
                if (!_isProcessingQueue)
                {
                    _isProcessingQueue = true;
                    await Task.Run(() => ProcessQueue());
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError(":- Pro_ReaccommodateToSpecific Failed :-" + reaccommodationms?.disruptedFlightDB?.FlightNumber ?? "");
                throw;
            }
        }
        private async Task ProcessSaveDataAndNotify(PNRDetails pnrDetails, Dictionary<string, string>? AppParameterList, NhbTemplateResponse nhbTemplate)
        {
            try
            {
                string Result = await _pNR_HandlerService.SaveDataAndNotify(pnrDetails, pnrDetails.disruptedFlightDB, AppParameterList, nhbTemplate);
            }
            catch (Exception ex)
            {
                await _logHelper.LogError(":- ProcessSaveDataAndNotify msg :- " + ex.Message+" :- "+JsonConvert.SerializeObject(pnrDetails?.disruptedFlightDB));
                await _logHelper.LogConsoleException(ex);
            }
        }
        private async Task ProcessQueue()
        {
            while (_taskQueue.TryDequeue(out var task))
            {
                _ = Task.Run(async () =>
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        await task();
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });
                await Task.Delay(TimeSpan.FromSeconds(15));
            }
            _isProcessingQueue = false;
        }
        private async Task NoPNRAvailable(DisruptedFlightDB? disruptedFlightDB)
        {
            disruptedFlightDB.RECOStatus = Enum_DisruptionType.Completed.ToString();
            disruptedFlightDB.ReasonForNotProcessing = Enum_DisruptionType.Processedsuccessfully.ToString();
            UpdateDisruptedFlights updateDisruptedFlights = UpdateModelDisruptedFlights(disruptedFlightDB, true);
            await _dataMsService.UpdateDisruptedflights(updateDisruptedFlights);
        }
        private async Task CatchException(DisruptedFlightDB? disruptedFlightDB, Exception ex, Dictionary<string, string>? AppParameterList, NhbTemplateResponse nhbTemplate)
        {
            disruptedFlightDB.RECOStatus = Enum_DisruptionType.NotProcessed.ToString();
            disruptedFlightDB.ReasonForNotProcessing = Enum_DisruptionType.NavitaireFailure.ToString();

            UpdateDisruptedFlights updateDisruptedFlights = UpdateModelDisruptedFlights(disruptedFlightDB, false);
            await _dataMsService.UpdateDisruptedflights(updateDisruptedFlights);
            string MessagePathway = AppParameterList[Enum_MessagePathway.MessagePathway.ToString()].ToLower().Trim();
            if (MessagePathway == Enum_MessagePathway.UcgMode.ToString().ToLower().Trim())
            {
                string template = await _uCGWebService.NavitaireFailureEmailTemplate(disruptedFlightDB, ex.Message);
                await _uCGWebService.sendTheEmailAsync(template,"");
                 
            }
            else
            {
                await _uCGWebService.sendTheEmailByNHub(disruptedFlightDB, nhbTemplate, Enum_Template.Reaccommodation_Failed.ToString(), "", ex.Message);
            }
            await _logHelper.LogConsoleException(ex);
            await _logHelper.LogInfo(ex.Message+":--:"+JsonConvert.SerializeObject(disruptedFlightDB));
            throw new InvalidOperationException(ex.Message);
        }
        private UpdateDisruptedFlights UpdateModelDisruptedFlights(DisruptedFlightDB disruptedFlightDB, bool IsDelayProcess)
        {
            UpdateDisruptedFlights disruptedFlightsModelResponse = new UpdateDisruptedFlights
            {
                FLTID = disruptedFlightDB.FLTID,
                AirlineCode = disruptedFlightDB.AirlineCode,
                FlightNumber = disruptedFlightDB.FlightNumber,
                FlightDate = disruptedFlightDB.FlightDate,
                Origin = disruptedFlightDB.Origin,
                Destination = disruptedFlightDB.Destination,
                STD = disruptedFlightDB.STD,
                ETD = disruptedFlightDB.ETD,
                STA = disruptedFlightDB.STA,
                ETA = disruptedFlightDB.ETA,
                DisruptionType = disruptedFlightDB.DisruptionType,
                RECOStatus = disruptedFlightDB.RECOStatus,
                IsApproved = disruptedFlightDB.IsApproved,
                Retry = disruptedFlightDB.Retry,
                Source = disruptedFlightDB.Source,
                ReasonForNotProcessing = disruptedFlightDB.ReasonForNotProcessing,
                ReaccommodatedPNRCount = disruptedFlightDB.ReaccommodatedPNRCount,
                NotReaccommodatedPNRCount = disruptedFlightDB.NotReaccommodatedPNRCount,
                DisruptionCode = disruptedFlightDB.DisruptionCode,
                IsDelayProcess = IsDelayProcess,
            };
            return disruptedFlightsModelResponse;
        }
    }
}
