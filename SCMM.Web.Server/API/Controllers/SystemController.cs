using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using SCMM.Web.Data.Models.UI.System;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Queries;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/system")]
    public class SystemController : ControllerBase
    {
        private readonly ILogger<SystemController> _logger;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public SystemController(ILogger<SystemController> logger, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// Get system status
        /// </summary>
        /// <remarks>Response is cached for 3mins</remarks>
        /// <param name="appId">The app to check the status of</param>
        /// <param name="includeAppStatus">If true, status details for the active steam app will be included</param>
        /// <param name="includeMarketStatus">If true, status details for all markets will be included</param>
        /// <param name="includeWebProxyStatus">If true, status details for web proxies will be included</param>
        /// <returns>The system status for the requested app</returns>
        /// <response code="200">The system status for the requested app.</response>
        /// <response code="404">If the system status is not currently available.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("status")]
        [ProducesResponseType(typeof(SystemStatusDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(PolicyName = CachePolicy.Expire3m, Tags = [CacheTag.System])]
        public async Task<IActionResult> GetSystemStatus([FromQuery] ulong? appId = null, [FromQuery] bool includeAppStatus = false, [FromQuery] bool includeMarketStatus = false, [FromQuery] bool includeWebProxyStatus = false)
        {
            var systemStatus = await _queryProcessor.ProcessAsync(new GetSystemStatusRequest()
            {
                AppId = appId ?? this.App().Id,
                IncludeAppStatus = includeAppStatus,
                IncludeMarketStatus = includeMarketStatus,
                IncludeWebProxyStatus = includeWebProxyStatus
            });
            if (systemStatus?.Status == null)
            {
                return NotFound("No system status information found");
            }

            return Ok(systemStatus.Status);
        }

        /// <summary>
        /// Get most recent system update messages
        /// </summary>
        /// <remarks>Response is cached for 1hr</remarks>
        /// <returns>The most recent system update messages</returns>
        /// <response code="200">The most recent system update messages</response>
        /// <response code="404">If the request app cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("latestUpdates")]
        [ProducesResponseType(typeof(IEnumerable<SystemUpdateMessageDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(PolicyName = CachePolicy.Expire1h, Tags = [CacheTag.System])]
        public async Task<IActionResult> GetLatestSystemUpdateMessages()
        {
            var latestSystemUpdates = await _queryProcessor.ProcessAsync(new ListLatestSystemUpdateMessagesRequest());
            if (latestSystemUpdates?.Messages == null)
            {
                return NotFound("No recent system changes found");
            }

            return Ok(latestSystemUpdates.Messages.ToArray());
        }
    }
}
