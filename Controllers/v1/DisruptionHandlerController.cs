using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RECO.DistrubtionHandler_MS.DistrubtionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.Models.RequestModel;
using RECO.DistrubtionHandler_MS.IUtilities;
using RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.Models.DatabaseModel;
using RECO.DistrubtionHandler_MS.Models.Enum;
using RECO.DistrubtionHandler_MS.DisruptionHandlerService;
using Microsoft.AspNetCore.Authorization;

namespace RECO.DistrubtionHandler_MS.Controllers.v1
{
    [Authorize(Policy = "CustomPolicy")]
    [Route("v1")]
    [ApiController]
    public class DisruptionHandlerController : ControllerBase
    {
        #region Private Properties and Variables
        private readonly ILogHelper _logHelper;
        private readonly IAuthService _authorizationService;   
        private readonly IReaccommodationMSService _recoReaccommodationMS;
        #endregion

        #region Constructor
        public DisruptionHandlerController(IAuthService authorizationService, ILogHelper logHelper, IReaccommodationMSService recoReaccommodation)
        {
            _logHelper = logHelper;
            _recoReaccommodationMS = recoReaccommodation;
            _authorizationService=authorizationService; 
        }
        #endregion

        #region Public EndPoint
        /// <summary>
        /// HandleDisruptedFlight
        /// </summary>
        /// <param name="HandleDisruptedFlight"></param>
        /// <returns></returns>
        /// 
        [HttpPost]
        [Route("HandleDisruptedFlight")]
        public async Task<IActionResult> HandleDisruptedFlight(DisruptedFlight disruptedFlight)
        {
            try
            {
                _logHelper.LogInfo("*******  Start HandleDisruptedFlight ******** ");
                _logHelper.LogInfo(JsonConvert.SerializeObject(disruptedFlight));
                bool Result =await _recoReaccommodationMS.InitiateReco(disruptedFlight);
                if(Result)
                {
                    _logHelper.LogInfo("*******  END HandleDisruptedFlight ******** "+ JsonConvert.SerializeObject(new { isSuccess = true, message = Enum_CustomMessage.Successful.ToString(), errorMessage = "" }, Formatting.Indented));
                    return Ok(JsonConvert.SerializeObject(new { isSuccess = true, message = Enum_CustomMessage.Successful.ToString(), errorMessage = "" }, Formatting.Indented)); 
                }
                else
                {
                    _logHelper.LogInfo("*******  END HandleDisruptedFlight ******** "+ JsonConvert.SerializeObject(new { isSuccess = false, message = Enum_CustomMessage.Failed.ToString(), errorMessage = "Initiate Reco Engine Failed." }, Formatting.Indented));
                    return Ok(JsonConvert.SerializeObject(new { isSuccess = false, message = Enum_CustomMessage.Failed.ToString(), errorMessage= Enum_CustomMessage.InitiateRecoEngineFailed.GetDescription()}, Formatting.Indented));
                }
                
            }
            catch (Exception ex)
            {
                _logHelper.LogConsoleException(ex);

                if (ex.Message == Enum_ReasonForNotProcessing.OutsideEligibilityWindow.ToString()) {
                    return Ok(JsonConvert.SerializeObject(new { isSuccess = true, message = Enum_ReasonForNotProcessing.OutsideEligibilityWindow.ToString(), errorMessage = "" }, Formatting.Indented));
                }
                else if (ex.Message == Enum_ReasonForNotProcessing.NoActionRequired.ToString())
                {
                    return Ok(JsonConvert.SerializeObject(new { isSuccess = true, message = Enum_ReasonForNotProcessing.NoActionRequired.ToString(), errorMessage = "" }, Formatting.Indented));
                }
                else if(ex.Message == Enum_CustomMessage.TripNotAvailable.GetDescription())
                {
                    return Ok(JsonConvert.SerializeObject(new { isSuccess = false, message = ex.Message, errorMessage = "" }, Formatting.Indented));
                }
                else
                {
                    return Ok(JsonConvert.SerializeObject(new { isSuccess = false, message = Enum_CustomMessage.Failed, errorMessage = ex.Message }, Formatting.Indented));
                }
                
            }
        }
        [HttpPost]
        [Route("PostFlightDetails")]
        public async Task<IActionResult> PostFlightDetails(FlightData flightData)
        {
            try
            {

                IdentifierResponse result = new IdentifierResponse();
                string? token = await _authorizationService.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    result = await _recoReaccommodationMS.GetFlightDetails(flightData, token);
                }
                string jsonResult = JsonConvert.SerializeObject(result, Formatting.Indented);
                return Ok(jsonResult);
            }
            catch (Exception ex)
            {
                _logHelper.LogConsoleException(ex);
                IdentifierResponse identifierResponse = new IdentifierResponse();
                identifierResponse.data = new FlightDetails();
                identifierResponse.isSuccess = false;
                identifierResponse.message = "Failed";
                identifierResponse.data = null;
                identifierResponse.errorMessage = ex.Message.Contains("status code") ? "System Error - Routes not Found" : ex.Message;
                string jsonResult = JsonConvert.SerializeObject(identifierResponse, Formatting.Indented);
                return Ok(jsonResult);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("RetryReco")]
        public async Task<IActionResult> RetryReco()
        {
            try
            {
                _logHelper.LogInfo("*******  Start RetryReco ******** ");
                List<DisruptedFlight> disruptedFlights = await _recoReaccommodationMS.cronJobSchedule();
                foreach(DisruptedFlight disrupted in disruptedFlights)
                {
                    bool Result = await _recoReaccommodationMS.RetryRECO(disrupted);
                }
                _logHelper.LogInfo("*******  End RetryReco ******** ");
                return Ok(JsonConvert.SerializeObject(new { isSuccess = true, message = "Again start Job", errorMessage = "" }, Formatting.Indented));
            }
            catch (Exception ex) {
                _logHelper.LogInfo("*******  End RetryReco ******** ");
                _logHelper.LogConsoleException(ex);
                return BadRequest(JsonConvert.SerializeObject(new { isSuccess = false, message = "Not start Job", errorMessage = ex.Message }, Formatting.Indented));
            }
        }
        #endregion
    }
}
