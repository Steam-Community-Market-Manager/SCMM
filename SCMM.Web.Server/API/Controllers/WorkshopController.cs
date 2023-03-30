using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models;
using SCMM.Web.Data.Models.Extensions;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Data.Models.UI.Workshop;
using SCMM.Web.Server.Extensions;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/workshop")]
    public class WorkshopController : ControllerBase
    {
        private readonly ILogger<WorkshopController> _logger;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public WorkshopController(ILogger<WorkshopController> logger, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// Search for workshop file submissions
        /// </summary>
        /// <param name="id">Optional array of ids to be included in the list. Helpful when you want search for items and compare against a known (but unrelated) item</param>
        /// <param name="filter">Optional search filter. Matches against item GUID, ID64, name, description, author, type, collection, and tags</param>
        /// <param name="type">If specified, only items matching the supplied item type are returned</param>
        /// <param name="collection">If specified, only items matching the supplied item collection are returned</param>
        /// <param name="creatorId">If specified, only items published by the supplied creator id are returned</param>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 5000.</param>
        /// <param name="sortBy">Sort item property name from <see cref="ItemDetailedDTO"/></param>
        /// <param name="sortDirection">Sort item direction</param>
        /// <response code="200">Paginated list of workshop files matching the request parameters.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="404">If no items match the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<WorkshopFileDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItems([FromQuery] string[] id = null, [FromQuery] string filter = null, [FromQuery] string type = null, [FromQuery] string collection = null, [FromQuery] ulong? creatorId = null,
                                                  [FromQuery] int start = 0, [FromQuery] int count = 10, [FromQuery] string sortBy = null, [FromQuery] SortDirection sortDirection = SortDirection.Ascending)
        {
            if (start < 0)
            {
                return BadRequest("Start index must be a positive number");
            }
            if (count <= 0)
            {
                count = int.MaxValue;
            }

            // Filter app
            var appId = this.App().Guid;
            var query = _db.SteamWorkshopFiles.AsNoTracking()
                .Where(x => x.AppId == appId);

            // Filter search
            filter = Uri.UnescapeDataString(filter?.Trim() ?? string.Empty);
            var filterWords = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var filterWord in filterWords)
            {
                query = query.Where(x =>
                    id.Contains(x.SteamId) ||
                    x.Id.ToString() == filterWord ||
                    x.SteamId == filterWord ||
                    x.Name.Contains(filterWord) ||
                    x.Description.Contains(filterWord) ||
                    (x.CreatorProfile != null && x.CreatorProfile.Name.Contains(filterWord)) ||
                    x.ItemType.Contains(filterWord) ||
                    x.ItemCollection.Contains(filterWord) ||
                    x.Tags.Serialised.Contains(filterWord)
                );
            }

            // Filter toggles
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(x => id.Contains(x.SteamId) || x.ItemType == type);
            }
            if (!string.IsNullOrEmpty(collection))
            {
                query = query.Where(x => id.Contains(x.SteamId) || x.ItemCollection == collection);
            }
            if (creatorId != null)
            {
                query = query.Where(x => id.Contains(x.SteamId) || x.CreatorId == creatorId || (x.CreatorProfile != null && x.CreatorProfile.SteamId == creatorId.ToString()) || (x.App != null && x.App.SteamId == creatorId.ToString()));
            }

            // Join
            query = query
                .Include(x => x.App)
                .Include(x => x.CreatorProfile);

            // Sort
            if (!String.IsNullOrEmpty(sortBy))
            {
                query = query.SortBy(sortBy, sortDirection);
            }
            else
            {
                query = query.OrderByDescending(x => x.TimeCreated);
            }

            // Paginate
            count = Math.Max(0, Math.Min(5000, count));
            return Ok(
                await query.PaginateAsync(start, count, x => _mapper.Map<SteamWorkshopFile, WorkshopFileDTO>(x, this))
            );
        }
    }
}
