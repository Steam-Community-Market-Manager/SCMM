using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Search;
using SCMM.Web.Server.Extensions;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly ILogger<AppController> _logger;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public SearchController(ILogger<AppController> logger, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// Search the databases for matching objects
        /// </summary>
        /// <remarks>Searches items and collections</remarks>
        /// <param name="query">The search query</param>
        /// <returns></returns>
        /// <response code="200">The top 10 results that match the search query</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SearchResultDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromQuery] string query)
        {
            var app = this.App();
            var words = query?.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (words?.Any() != true)
            {
                return Ok();
            }

            var results = new List<SearchResultDTO>();

            var itemTypeResults = await _db.SteamAssetDescriptions
                .Where(x => x.AppId == app.Guid)
                .Where(x => !String.IsNullOrEmpty(x.ItemType))
                .GroupBy(x => x.ItemType)
                .Select(x => x.Key)
                .ToListAsync();

            var itemCollectionResults = await _db.SteamAssetDescriptions
                .Where(x => x.AppId == app.Guid)
                .Where(x => !String.IsNullOrEmpty(x.ItemCollection))
                .GroupBy(x => new
                {
                    CreatorId = x.CreatorId,
                    ItemCollection = x.ItemCollection
                })
                .Select(x => new
                {
                    CreatorId = x.Key.CreatorId,
                    ItemCollection = x.Key.ItemCollection,
                    ItemIconUrl = x.FirstOrDefault().IconUrl
                })
                .ToListAsync();

            var itemResults = await _db.SteamAssetDescriptions
                .Where(x => x.AppId == app.Guid)
                .Where(x => !String.IsNullOrEmpty(x.Name))
                .Select(x => new
                {
                    ClassId = x.ClassId,
                    Name = x.Name,
                    IconUrl = x.IconUrl
                })
                .ToListAsync();

            results.AddRange(
                itemTypeResults.Select(x => new SearchResultDTO()
                {
                    Type = "Type",
                    IconUrl = $"/images/app/{app.Id}/items/{x.ToRustItemShortName()}.png",
                    Description = x,
                    Url = $"api/item?type={x}&count=-1"
                })
            );

            results.AddRange(
                itemCollectionResults.Select(x => new SearchResultDTO()
                {
                    Type = "Collection",
                    IconUrl = x.ItemIconUrl,
                    Description = x.ItemCollection,
                    Url = $"api/item/collection/{x.ItemCollection}?creatorId={x.CreatorId}"
                })
            );

            results.AddRange(
                itemResults.Select(x => new SearchResultDTO()
                {
                    Type = "Item",
                    IconUrl = x.IconUrl,
                    Description = x.Name,
                    Url = $"/api/item/{x.ClassId}"
                })
            );

            return Ok(results
                .Where(x => words.Any(y => x.Description.Contains(y, StringComparison.InvariantCultureIgnoreCase)))
                .OrderBy(x => x.Description.LevenshteinDistanceFrom(query))
                .Take(10)
                .ToArray()
            );
        }
    }
}
