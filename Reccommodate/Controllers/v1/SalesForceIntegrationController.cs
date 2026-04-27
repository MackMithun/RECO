using Microsoft.AspNetCore.Mvc;
using RECO.Reaccommodation_MS.ReaccommodationService.Interface;
using RECO.Reaccommodation_MS.IUtilities;

namespace RECO.Reaccommodation_MS.Controllers.v1
{
    [Route("v1")]
    [ApiController]
    public class SalesForceIntegrationController : ControllerBase
    {
        private readonly ISalesForceService _cApiService;
        private readonly ILogHelper _logHelper;

        public SalesForceIntegrationController(ISalesForceService cApiService, ILogHelper logHelper)
        {
            _cApiService = cApiService ?? throw new ArgumentNullException(nameof(cApiService));
            _logHelper = logHelper;
        }

        [HttpGet]
        [Route("AccessToken")]
        public async Task<IActionResult> GetAccessToken()
        {
            try
            {
                var tokenResponse = await _cApiService.GetAccessTokenAsync();
                return Ok(tokenResponse);
            }
            catch (Exception ex)
            {
                _logHelper.LogConsoleException(ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("Composite")]
        public async Task<IActionResult> ExecuteCompositeRequest([FromBody] object compositeRequestBody)
        {
            try
            {
                var tokenResponse = await _cApiService.GetAccessTokenAsync();
                var responseBody = await _cApiService.ExecuteCompositeRequestAsync(tokenResponse.access_token, compositeRequestBody);
                return Ok(responseBody);
            }
            catch (Exception ex)
            {
                _logHelper.LogConsoleException(ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("CompositeCopy")]
        public async Task<IActionResult> ExecuteCompositeCopyRequest([FromBody] object compositeRequestBody)
        {
            try
            {
                var tokenResponse = await _cApiService.GetAccessTokenAsync();
                var response = await _cApiService.ExecuteCompositeCopyRequestAsync(tokenResponse.access_token, compositeRequestBody);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logHelper.LogConsoleException(ex);
                return BadRequest(ex.Message);
            }
        }
    }
}
