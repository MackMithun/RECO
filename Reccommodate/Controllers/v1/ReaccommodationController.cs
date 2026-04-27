using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;
using RECO.Reaccommodation_MS.Models.RequestModel;
using System.Collections.Concurrent;
using RECO.Reaccommodation_MS.Models.ResponseModel;

namespace RECO.ReaccommodationDALService.Controllers.v1
{
    [Route("v1")]
    [ApiController]
    public class ReaccommodationController : ControllerBase
    {
        #region Private Properties and Variables
        private readonly INavitaireAuthorizationService _authorizationService;
        private readonly ISuitableFlightService _suitableFlightService;
        private readonly ISpecificFlightService _specificFlightService;
        private readonly IDashboardDetailsService _dashboardDetailsService;  
        private readonly ILogHelper _logHelper;
        private readonly string _caller = typeof(ReaccommodationController).Name;
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(20, 20); // Allows only Ten task at a time
        private static ConcurrentQueue<Func<Task>> _taskQueue = new ConcurrentQueue<Func<Task>>();
        private static HashSet<DisruptedFlight> TaskdisruptedFlights = new HashSet<DisruptedFlight>();  
        private static bool _isProcessingQueue = false;
        #endregion

        #region Constructor
        public ReaccommodationController(INavitaireAuthorizationService authorizationService, ISuitableFlightService suitableFlightService, ISpecificFlightService specificFlightService, ILogHelper logHelper, IDashboardDetailsService dashboardDetailsService)
        {
            _authorizationService = authorizationService;
            _suitableFlightService = suitableFlightService;
            _specificFlightService = specificFlightService;     
            _logHelper = logHelper;
            _dashboardDetailsService= dashboardDetailsService;      
        }
        #endregion
        [HttpPost]
        [Route("PostDashboardDetails")]
        public IActionResult PostDashboardDetails(DashboardDetails dashboardDetails)
        {
            try
            {
                Task.Run(() => _dashboardDetailsService.fnGenrateDashboardDetails(dashboardDetails));
                return Ok(JsonConvert.SerializeObject(new
                {
                    isSuccess = true,
                    message = "The report generation has started and will be sent to this email ID: " + dashboardDetails.EmailID + " within 5 minutes.",
                    errorMessage = ""
                }, Formatting.Indented));
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{_caller}{"PostDashboardDetails"}{" msg:-"} {ex.Message}:- {"End "}");
                _logHelper.LogConsoleException(ex);
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("GetDashboardDetails")]
        public async Task<IActionResult> GetDashboardDetails()
        {
            try
            {
                DashboardResponse dashboardDetails = await _dashboardDetailsService.fnDashboardDetails();
                var response = new
                {
                    isSuccess = true,
                    data = dashboardDetails,   
                    errorMessage = ""          
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}{"PostDashboardDetails"}{" msg:-"} {ex.Message}:- {"End "}");
                await _logHelper.LogConsoleException(ex);
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("ReaccomodatePnrsForFlight")]
        public IActionResult ReaccomodatePnrsForFlight([FromBody] DisruptedFlight disruptedFlight)
        {
            try
            {
                var ObjdisruptedFlight=new DisruptedFlight
                {
                    origin = disruptedFlight.origin,
                    destination = disruptedFlight.destination,  
                    carrierCode = disruptedFlight.carrierCode,  
                    beginDate = disruptedFlight.beginDate,  
                    oldIdentifier = disruptedFlight.oldIdentifier,
                    flightType = disruptedFlight.flightType,    
                    newIdentifier = disruptedFlight.newIdentifier,    
                };
                if(TaskdisruptedFlights.Add(ObjdisruptedFlight))
                {
                    _taskQueue.Enqueue(async () => await ProcessDisruptedFlight(disruptedFlight, ObjdisruptedFlight));
                    if (!_isProcessingQueue)
                    {
                        _isProcessingQueue = true;
                        Task.Run(() => ProcessQueue());
                    }
                    return Ok(JsonConvert.SerializeObject(new { isSuccess = true, message = "Initiated Reco Engine successfully." }, Formatting.Indented));
                }
                else
                {
                    return Ok(JsonConvert.SerializeObject(new { isSuccess = true, message = "Initiated Reco Engine successfully.." }, Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogConsoleException(ex);
                return BadRequest(ex.Message);
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
                        if(task is Func<Task> taskfunc)
                        {
                            dynamic disruptedFlightTask = taskfunc?.Target ?? null;
                            if (disruptedFlightTask != null)
                            {
                                TaskdisruptedFlights.Remove(disruptedFlightTask.ObjdisruptedFlight);
                            }
                        }
                        _semaphore.Release();
                        
                    }
                });
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
            _isProcessingQueue = false; // Reset the flag when done processing
        }
        private async Task ProcessDisruptedFlight(DisruptedFlight disruptedFlight, DisruptedFlight TasKdisrupted)
        {
            try
            {
                if (disruptedFlight != null)
                {
                    await _logHelper.LogInfo($"{_caller} ReaccomodatePnrsForFlight :- Start");
                    await _logHelper.LogInfo($"{_caller} HASH SET TaskdisruptedFlights Count ="+ TaskdisruptedFlights.Count());
                    await _logHelper.LogInfo(JsonConvert.SerializeObject(disruptedFlight));
                    string? Token = await _authorizationService.GetTokenAsync();
                    if (!string.IsNullOrEmpty(Token))
                    {
                        if (!string.IsNullOrEmpty(disruptedFlight.newIdentifier))
                        {
                            await _logHelper.LogInfo("2. Reco MovePNRToGivenFlight start");
                            await _specificFlightService.ReaccommodateToSpecificFlight(disruptedFlight, Token);
                            await _logHelper.LogInfo("2. Reco MovePNRToGivenFlight End");
                        }
                        else
                        {
                            await _logHelper.LogInfo("2. Reco MovePNRToSuitableFlight start");
                            await _suitableFlightService.ReaccommodateToSuitableFlight(disruptedFlight, Token);
                            await _logHelper.LogInfo("2. Reco MovePNRToSuitableFlight End");
                        }
                    }
                    else
                    {
                        await _logHelper.LogInfo($"{_caller} ReaccomodatePnrsForFlight :- End : Token not Created");
                    }
                }
                await _logHelper.LogInfo($"{_caller} ReaccomodatePnrsForFlight :- End");
            }
            catch (Exception ex)
            {
                await _logHelper.LogConsoleException(ex);
            }
        }
    }
}