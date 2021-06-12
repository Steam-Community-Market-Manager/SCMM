using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Extensions;
using SCMM.Shared.Web.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models;
using SCMM.Web.Data.Models.Domain.InventoryItems;
using SCMM.Web.Data.Models.Domain.Profiles;
using SCMM.Web.Data.Models.Extensions;
using SCMM.Web.Data.Models.UI.ProfileInventory;
using SCMM.Web.Server.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/profile")]
    public class ProfileController : ControllerBase
    {
        private readonly ILogger<ProfileController> _logger;
        private readonly IConfiguration _configuration;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public ProfileController(ILogger<ProfileController> logger, IConfiguration configuration, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _configuration = configuration;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// Get your profile information
        /// </summary>
        /// <remarks>
        /// The language used for text localisation can be changed by defining the <code>Language</code> header and setting it to a supported language identifier (e.g. 'english').
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <response code="200">If the session is authentication, your Steam profile information is returned. If the session is unauthenticated, a generic guest profile is returned.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(ProfileDetailedDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyProfile()
        {
            var defaultProfile = new ProfileDetailedDTO()
            {
                Name = "Guest",
                Language = this.Language(),
                Currency = this.Currency()
            };

            // If the user is authenticated, use their database profile
            if (User.Identity.IsAuthenticated)
            {
                var profileId = User.Id();
                var profile = await _db.SteamProfiles
                    .AsNoTracking()
                    .Include(x => x.Language)
                    .Include(x => x.Currency)
                    .FirstOrDefaultAsync(x => x.Id == profileId);

                // Map the DB profile over top of the default profile
                // NOTE: This is done so that the language/currency pass-through if they haven't been set yet
                var authenticatedProfile = _mapper.Map<ProfileDetailedDTO>(profile);
                if (authenticatedProfile != null)
                {
                    authenticatedProfile.Language = (authenticatedProfile.Language ?? defaultProfile.Language);
                    authenticatedProfile.Currency = (authenticatedProfile.Currency ?? defaultProfile.Currency);
                    return Ok(authenticatedProfile);
                }
                else
                {
                    return Ok(defaultProfile);
                }
            }

            // Else, use a transient guest profile
            else
            {
                return Ok(defaultProfile);
            }
        }

        /// <summary>
        /// Update your profile information
        /// </summary>
        /// <remarks>This API requires authentication</remarks>
        /// <param name="command">
        /// Information to be updated to your profile. 
        /// Any fields that are <code>null</code> are ignored (not updated).
        /// </param>
        /// <response code="200">If the profile was updated successfully.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="401">If the request is unauthenticated (login first).</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(AuthorizationPolicies.User)]
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SetMyProfile([FromBody] UpdateProfileCommand command)
        {
            if (command == null)
            {
                return BadRequest($"No data was supplied to be updated");
            }

            var profileId = User.Id();
            var profile = await _db.SteamProfiles
                .Include(x => x.Language)
                .Include(x => x.Currency)
                .FirstOrDefaultAsync(x => x.Id == profileId);

            if (profile == null)
            {
                return BadRequest($"Profile {profileId} was not found");
            }

            if (command.DiscordId != null)
            {
                profile.DiscordId = command.DiscordId;
            }
            if (command.Language != null)
            {
                profile.Language = _db.SteamLanguages.FirstOrDefault(x => x.Name == command.Language);
            }
            if (command.Currency != null)
            {
                profile.Currency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == command.Currency);
            }
            if (command.TradeUrl != null)
            {
                profile.TradeUrl = command.TradeUrl;
            }
            if (command.GamblingOffset != null)
            {
                profile.GamblingOffset = command.GamblingOffset.Value;
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Get Steam profile information
        /// </summary>
        /// <param name="steamId">Valid SteamId (int64), ProfileId (string), or Steam profile page URL</param>
        /// <response code="200">Steam profile information.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="404">If the profile cannot be found or the profile is private.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{steamId}/summary")]
        [ProducesResponseType(typeof(ProfileSummaryDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfileSummary([FromRoute] string steamId)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return BadRequest("Profile id is invalid");
            }

            // Load the profile
            var importedProfile = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
            {
                ProfileId = steamId
            });

            var profile = importedProfile?.Profile;
            if (profile == null)
            {
                return NotFound($"Profile not found");
            }

            await _db.SaveChangesAsync();
            return Ok(
                _mapper.Map<SteamProfile, ProfileSummaryDTO>(
                    profile, this
                )
            );
        }

        #region Inventory Items

        /// <summary>
        /// Synchronise Steam profile inventory
        /// </summary>
        /// <param name="steamId">Valid SteamId (int64), ProfileId (string), or Steam profile page URL</param>
        /// <param name="force">If true, the inventory will always be fetched from Steam. If false, sync calls to Steam are cached for one hour</param>
        /// <response code="200">If the inventory was successfully synchronised.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="404">If the profile cannot be found or is the inventory is private.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpPost("{steamId}/inventory/sync")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostInventorySync([FromRoute] string steamId, [FromQuery] bool force = false)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return BadRequest("Profile id is invalid");
            }

            // Reload the profile's inventory
            var importedInventory = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileInventoryRequest()
            {
                ProfileId = steamId,
                Force = force
            });

            var profile = importedInventory?.Profile;
            if (profile == null)
            {
                return NotFound($"Profile with SteamID '{steamId}' was not found");
            }
            if (profile?.Privacy != SteamVisibilityType.Public)
            {
                return NotFound($"Profile with SteamID '{steamId}' inventory is private");
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Get Steam profile inventory and calculate the market value
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// Inventory mosaic images automatically expire after 7 days; After which, the URL will return a 404 response.
        /// </remarks>
        /// <param name="steamId">Valid SteamId (int64), ProfileId (string), or Steam profile page URL</param>
        /// <param name="generateInventoryMosaic">If true, a mosaic image of the highest valued items will be generated in the response. If false, the mosaic will be <code>null</code>.</param>
        /// <param name="mosaicTileSize">The size (in pixel) to render each item within the mosaic image (if enabled)</param>
        /// <param name="mosaicColumns">The number of item columns to render within the mosaic image (if enabled)</param>
        /// <param name="mosaicRows">The number of item rows to render within the mosaic image (if enabled)</param>
        /// <param name="force">If true, the inventory will always be fetched from Steam. If false, calls to Steam are cached for one hour</param>
        /// <response code="200">Profile inventory value.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="404">If the profile cannot be found or if the inventory is private/empty.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{steamId}/inventory/value")]
        [ProducesResponseType(typeof(ProfileInventoryValueDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInventoryValue([FromRoute] string steamId, [FromQuery] bool generateInventoryMosaic = false, [FromQuery] int mosaicTileSize = 128, [FromQuery] int mosaicColumns = 5, [FromQuery] int mosaicRows = 5, [FromQuery] bool force = false)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return BadRequest("Profile id is invalid");
            }

            // Load the profile
            var importedProfile = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
            {
                ProfileId = steamId
            });

            var profile = importedProfile?.Profile;
            if (profile == null)
            {
                return NotFound("Profile not found");
            }

            // Reload the profiles inventory
            var importedInventory = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileInventoryRequest()
            {
                ProfileId = profile.Id.ToString(),
                Force = force
            });
            if (importedInventory?.Profile?.Privacy != SteamVisibilityType.Public)
            {
                return NotFound("Profile inventory is private");
            }

            await _db.SaveChangesAsync();

            // Calculate the profiles inventory totals
            var inventoryTotals = await _queryProcessor.ProcessAsync(new GetSteamProfileInventoryTotalsRequest()
            {
                ProfileId = profile.SteamId,
                CurrencyId = this.Currency()?.SteamId,
            });
            if (inventoryTotals == null)
            {
                return NotFound("Profile inventory is empty (no marketable items)");
            }

            // Generate the profiles inventory thumbnail
            var inventoryThumbnail = (GenerateSteamProfileInventoryThumbnailResponse)null;
            if (generateInventoryMosaic)
            {
                inventoryThumbnail = await _commandProcessor.ProcessWithResultAsync(new GenerateSteamProfileInventoryThumbnailRequest()
                {
                    ProfileId = profile.SteamId,
                    TileSize = mosaicTileSize,
                    Columns = mosaicColumns,
                    Rows = mosaicRows,
                    ExpiresOn = DateTimeOffset.Now.AddDays(7)
                });
            }

            await _db.SaveChangesAsync();

            return Ok(
                new ProfileInventoryValueDTO()
                {
                    SteamId = profile.SteamId,
                    Name = profile.Name,
                    AvatarUrl = profile.AvatarUrl,
                    InventoryMosaicUrl = inventoryThumbnail != null ? $"{_configuration.GetWebsiteUrl()}/api/image/{inventoryThumbnail.Image?.Id}" : null,
                    Items = inventoryTotals.TotalItems,
                    Invested = inventoryTotals.TotalInvested,
                    MarketValue = inventoryTotals.TotalMarketValue,
                    Market24hrMovement = inventoryTotals.TotalMarket24hrMovement,
                    ResellValue = inventoryTotals.TotalResellValue,
                    ResellTax = inventoryTotals.TotalResellTax,
                    ResellProfit = inventoryTotals.TotalResellProfit,
                    Currency = this.Currency()
                }
            );
        }

        /// <summary>
        /// Get profile inventory item totals
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="steamId">Valid SteamId (int64), ProfileId (string), or Steam profile page URL</param>
        /// <response code="200">Profile inventory item totals.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="404">If the profile cannot be found or if the inventory is private/empty.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{steamId}/inventory/total")]
        [ProducesResponseType(typeof(ProfileInventoryTotalsDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInventoryTotal([FromRoute] string steamId)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return BadRequest("Profile id is invalid");
            }

            var inventoryTotals = await _queryProcessor.ProcessAsync(new GetSteamProfileInventoryTotalsRequest()
            {
                ProfileId = steamId,
                CurrencyId = this.Currency().SteamId
            });
            if (inventoryTotals == null)
            {
                return NotFound("Profile inventory is empty (no marketable items)");
            }

            return Ok(
                _mapper.Map<GetSteamProfileInventoryTotalsResponse, ProfileInventoryTotalsDTO>(inventoryTotals, this)
            );
        }

        /// <summary>
        /// Get profile inventory item information
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="steamId">Valid SteamId (int64), ProfileId (string), or Steam profile page URL</param>
        /// <response code="200">Profile inventory item information.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{steamId}/inventory/items")]
        [ProducesResponseType(typeof(IList<ProfileInventoryItemSummaryDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInventoryItems([FromRoute] string steamId)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return BadRequest("Profile id is invalid");
            }

            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = steamId
            });
            if (resolvedId?.Exists != true)
            {
                return NotFound("Profile not found");
            }

            var profileInventoryItems = await _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => x.Description != null)
                .Include(x => x.Description)
                .Include(x => x.Description.App)
                .Include(x => x.Description.StoreItem)
                .Include(x => x.Description.StoreItem.Prices)
                .Include(x => x.Description.MarketItem)
                .Include(x => x.Description.MarketItem.Currency)
                .ToListAsync();

            var profileInventoryItemsSummaries = new List<ProfileInventoryItemSummaryDTO>();
            foreach (var item in profileInventoryItems)
            {
                if (!profileInventoryItemsSummaries.Any(x => x.SteamId == item.Description.ClassId.ToString()))
                {
                    var itemSummary = _mapper.Map<SteamAssetDescription, ProfileInventoryItemSummaryDTO>(
                        item.Description, this
                    );

                    // Calculate the item's value
                    if (item.Description.MarketItem?.Last1hrValue > 0 && item.Description.MarketItem.Currency != null)
                    {
                        itemSummary.Value = this.Currency().CalculateExchange(
                            item.Description.MarketItem.Last1hrValue, item.Description.MarketItem.Currency
                        );
                    }
                    else if (item.Description.StoreItem?.Prices != null)
                    {
                        itemSummary.Value = (long?) item.Description.StoreItem.Prices.FirstOrDefault(x => x.Key == this.Currency().Name).Value;
                    }

                    // Calculate the item's quantity
                    itemSummary.Quantity = profileInventoryItems
                        .Where(x => x.Description.ClassId == item.Description.ClassId)
                        .Sum(x => x.Quantity);

                    // Calculate the item's flags
                    itemSummary.Flags = profileInventoryItems
                        .Where(x => x.Description.ClassId == item.Description.ClassId)
                        .Select(x => x.Flags)
                        .Aggregate((x, y) => x | y);

                    profileInventoryItemsSummaries.Add(itemSummary);
                }
            }

            return Ok(
                profileInventoryItemsSummaries.OrderByDescending(x => x.Value)
            );
        }

        /// <summary>
        /// Get profile inventory investment information
        /// </summary>
        /// <remarks>
        /// This API requires authentication.
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="steamId">Valid SteamId (int64), ProfileId (string), or Steam profile page URL</param>
        /// <param name="filter">Optional search filter. Matches against item name or description</param>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <param name="sortBy">Sort item property name from <see cref="InventoryInvestmentItemDTO"/></param>
        /// <param name="sortDirection">Sort item direction</param>
        /// <response code="200">Profile inventory investment information.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="401">If the request is unauthenticated (login first) or the requested inventory does not belong to the authenticated user.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(AuthorizationPolicies.User)]
        [HttpGet("{steamId}/inventory/investment")]
        [ProducesResponseType(typeof(PaginatedResult<InventoryInvestmentItemDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInventoryInvestment([FromRoute] string steamId, [FromQuery] string filter = null, [FromQuery] int start = 0, [FromQuery] int count = 10, [FromQuery] string sortBy = null, [FromQuery] SortDirection sortDirection = SortDirection.Ascending)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return BadRequest("Profile id is invalid");
            }

            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = steamId
            });
            if (resolvedId?.Exists != true || resolvedId.ProfileId == null)
            {
                return NotFound("Profile not found");
            }

            if (!User.Is(resolvedId.ProfileId.Value) && !User.IsInRole(Roles.Administrator))
            {
                _logger.LogError($"Inventory does not belong to you and you do not have permission to view it");
                return Unauthorized($"Inventory does not belong to you and you do not have permission to view it");
            }

            filter = Uri.UnescapeDataString(filter?.Trim() ?? String.Empty);
            var query = _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => String.IsNullOrEmpty(filter) || x.Description.Name.ToLower().Contains(filter.ToLower()))
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description.MarketItem)
                .Include(x => x.Description.MarketItem.Currency)
                .Include(x => x.Description.StoreItem)
                .Include(x => x.Description.StoreItem.Currency)
                .OrderBy(sortBy, sortDirection);

            var results = await query.PaginateAsync(start, count,
                x => _mapper.Map<SteamProfileInventoryItem, InventoryInvestmentItemDTO>(x, this)
            );

            return Ok(results);
        }

        /// <summary>
        /// Get profile inventory wishlist information
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="steamId">Valid SteamId (int64), ProfileId (string), or Steam profile page URL</param>
        /// <response code="200">Profile inventory wishlist information.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{steamId}/inventory/wishlist")]
        [ProducesResponseType(typeof(IList<ProfileInventoryItemWishDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInventoryWishlist([FromRoute] string steamId)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return BadRequest("Profile id is invalid");
            }

            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = steamId
            });
            if (resolvedId?.Exists != true)
            {
                return NotFound("Profile not found");
            }

            var profileMarketItems = await _db.SteamProfileMarketItems
                .AsNoTracking()
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => x.Flags.HasFlag(SteamProfileMarketItemFlags.WantToBuy))
                .Include(x => x.App)
                .Include(x => x.Description.MarketItem)
                .Include(x => x.Description.MarketItem.Currency)
                .OrderBy(x => x.Description.Name)
                .ToListAsync();

            var profileInventoryItemWishes = new List<ProfileInventoryItemWishDTO>();
            foreach (var profileMarketItem in profileMarketItems)
            {
                profileInventoryItemWishes.Add(
                    _mapper.Map<SteamProfileMarketItem, ProfileInventoryItemWishDTO>(
                        profileMarketItem, this
                    )
                );
            }

            return Ok(profileInventoryItemWishes);
        }

        /// <summary>
        /// Get profile inventory performance information
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="steamId">Valid SteamId (int64), ProfileId (string), or Steam profile page URL</param>
        /// <response code="200">Profile inventory performance information.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="404">If the profile cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{steamId}/inventory/performance")]
        [ProducesResponseType(typeof(ProfileInventoryPerformanceDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInventoryPerformance([FromRoute] string steamId)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return BadRequest("Profile id is invalid");
            }

            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = steamId
            });
            if (resolvedId?.Exists != true)
            {
                return NotFound("Profile not found");
            }

            var currency = this.Currency();
            var profileInventoryItems = await _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => x.Description.MarketItem != null)
                .Select(x => new
                {
                    Quantity = x.Quantity,
                    BuyPrice = x.BuyPrice,
                    ExchangeRateMultiplier = (x.Currency != null ? x.Currency.ExchangeRateMultiplier : 0),
                    MarketItemLast1hrValue = x.Description.MarketItem.Last1hrValue,
                    MarketItemLast24hrValue = x.Description.MarketItem.Last24hrValue,
                    MarketItemLast48hrValue = x.Description.MarketItem.Last48hrValue,
                    MarketItemLast72hrValue = x.Description.MarketItem.Last72hrValue,
                    MarketItemLast96hrValue = x.Description.MarketItem.Last96hrValue,
                    MarketItemLast120hrValue = x.Description.MarketItem.Last120hrValue,
                    MarketItemLast144hrValue = x.Description.MarketItem.Last144hrValue,
                    MarketItemLast168hrValue = x.Description.MarketItem.Last168hrValue,
                    MarketItemExchangeRateMultiplier = (x.Description.MarketItem.Currency != null ? x.Description.MarketItem.Currency.ExchangeRateMultiplier : 0)
                })
                .ToListAsync();

            var profileInventory = new
            {
                Invested = profileInventoryItems
                    .Where(x => x.BuyPrice != null && x.BuyPrice != 0 && x.ExchangeRateMultiplier != 0)
                    .Sum(x => (x.BuyPrice / x.ExchangeRateMultiplier) * x.Quantity),
                Last1hrValue = profileInventoryItems
                    .Where(x => x.MarketItemLast1hrValue != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemLast1hrValue / x.MarketItemExchangeRateMultiplier) * x.Quantity),
                Last24hrValue = profileInventoryItems
                    .Where(x => x.MarketItemLast24hrValue != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemLast24hrValue / x.MarketItemExchangeRateMultiplier) * x.Quantity),
                Last48hrValue = profileInventoryItems
                    .Where(x => x.MarketItemLast48hrValue != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemLast48hrValue / x.MarketItemExchangeRateMultiplier) * x.Quantity),
                Last72hrValue = profileInventoryItems
                    .Where(x => x.MarketItemLast72hrValue != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemLast72hrValue / x.MarketItemExchangeRateMultiplier) * x.Quantity),
                Last96hrValue = profileInventoryItems
                    .Where(x => x.MarketItemLast96hrValue != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemLast96hrValue / x.MarketItemExchangeRateMultiplier) * x.Quantity),
                Last120hrValue = profileInventoryItems
                    .Where(x => x.MarketItemLast120hrValue != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemLast120hrValue / x.MarketItemExchangeRateMultiplier) * x.Quantity),
                Last144hrValue = profileInventoryItems
                    .Where(x => x.MarketItemLast144hrValue != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemLast144hrValue / x.MarketItemExchangeRateMultiplier) * x.Quantity),
                Last168hrValue = profileInventoryItems
                    .Where(x => x.MarketItemLast168hrValue != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemLast168hrValue / x.MarketItemExchangeRateMultiplier) * x.Quantity)
            };

            var today = DateTimeOffset.UtcNow.Date;
            var totalInvested = currency.CalculateExchange(profileInventory.Invested ?? 0);
            var last168hrValue = currency.CalculateExchange(profileInventory.Last168hrValue);
            var last144hrValue = currency.CalculateExchange(profileInventory.Last144hrValue);
            var last120hrValue = currency.CalculateExchange(profileInventory.Last120hrValue);
            var last96hrValue = currency.CalculateExchange(profileInventory.Last96hrValue);
            var last72hrValue = currency.CalculateExchange(profileInventory.Last72hrValue);
            var last48hrValue = currency.CalculateExchange(profileInventory.Last48hrValue);
            var last24hrValue = currency.CalculateExchange(profileInventory.Last24hrValue);
            var last1hrValue = currency.CalculateExchange(profileInventory.Last1hrValue);

            var valueHistory = new Dictionary<DateTimeOffset, long>();
            valueHistory[today.Subtract(TimeSpan.FromDays(7))] = last168hrValue;
            valueHistory[today.Subtract(TimeSpan.FromDays(6))] = last144hrValue;
            valueHistory[today.Subtract(TimeSpan.FromDays(5))] = last120hrValue;
            valueHistory[today.Subtract(TimeSpan.FromDays(4))] = last96hrValue;
            valueHistory[today.Subtract(TimeSpan.FromDays(3))] = last72hrValue;
            valueHistory[today.Subtract(TimeSpan.FromDays(2))] = last48hrValue;
            valueHistory[today.Subtract(TimeSpan.FromDays(1))] = last24hrValue;
            valueHistory[today.Subtract(TimeSpan.FromDays(0))] = last1hrValue;

            var profitHistory = new Dictionary<DateTimeOffset, long>();
            profitHistory[today.Subtract(TimeSpan.FromDays(7))] = last168hrValue - last168hrValue.SteamFeeAsInt() - totalInvested;
            profitHistory[today.Subtract(TimeSpan.FromDays(6))] = last144hrValue - last144hrValue.SteamFeeAsInt() - totalInvested;
            profitHistory[today.Subtract(TimeSpan.FromDays(5))] = last120hrValue - last120hrValue.SteamFeeAsInt() - totalInvested;
            profitHistory[today.Subtract(TimeSpan.FromDays(4))] = last96hrValue - last96hrValue.SteamFeeAsInt() - totalInvested;
            profitHistory[today.Subtract(TimeSpan.FromDays(3))] = last72hrValue - last72hrValue.SteamFeeAsInt() - totalInvested;
            profitHistory[today.Subtract(TimeSpan.FromDays(2))] = last48hrValue - last48hrValue.SteamFeeAsInt() - totalInvested;
            profitHistory[today.Subtract(TimeSpan.FromDays(1))] = last24hrValue - last24hrValue.SteamFeeAsInt() - totalInvested;
            profitHistory[today.Subtract(TimeSpan.FromDays(0))] = last1hrValue - last1hrValue.SteamFeeAsInt() - totalInvested;

            return Ok(
                new ProfileInventoryPerformanceDTO()
                {
                    ValueHistoryGraph = valueHistory.ToDictionary(
                        x => x.Key.ToString("dd MMM yyyy"),
                        x => x.Value
                    ),
                    ProfitHistoryGraph = profitHistory.ToDictionary(
                        x => x.Key.ToString("dd MMM yyyy"),
                        x => x.Value
                    )
                }
            );
        }

        /// <summary>
        /// Update profile inventory item information
        /// </summary>
        /// <remarks>This API requires authentication</remarks>
        /// <param name="steamId">Valid SteamId (int64), ProfileId (string), or Steam profile page URL</param>
        /// <param name="itemId">
        /// Inventory item identifier to be updated. 
        /// The item must belong to your (currently authenticated) profile
        /// </param>
        /// <param name="command">
        /// Information to be updated for the item. 
        /// Any fields that are <code>null</code> are ignored (not updated).
        /// </param>
        /// <response code="200">If the inventory item was updated successfully.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="401">If the request is unauthenticated (login first) or the requested inventory item does not belong to the authenticated user.</response>
        /// <response code="404">If the inventory item cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(AuthorizationPolicies.User)]
        [HttpPut("{steamId}/inventory/item/{itemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SetInventoryItem([FromRoute] string steamId, [FromRoute] Guid itemId, [FromBody] UpdateInventoryItemCommand command)
        {
            if (command == null)
            {
                return BadRequest($"No data was supplied to be updated");
            }
            if (itemId == Guid.Empty)
            {
                return BadRequest("Inventory item id is invalid");
            }

            var inventoryItem = await _db.SteamProfileInventoryItems.FirstOrDefaultAsync(x => x.Id == itemId);
            if (inventoryItem == null)
            {
                _logger.LogError($"Inventory item with id '{itemId}' was not found");
                return NotFound($"Inventory item with id '{itemId}' was not found");
            }
            if (!User.Is(inventoryItem.ProfileId) && !User.IsInRole(Roles.Administrator))
            {
                _logger.LogError($"Inventory item with id '{itemId}' does not belong to you and you do not have permission to modify it");
                return Unauthorized($"Inventory item with id '{itemId}' does not belong to you and you do not have permission to modify it");
            }

            if (command.CurrencyId != Guid.Empty)
            {
                inventoryItem.CurrencyId = command.CurrencyId;
                inventoryItem.BuyPrice = command.BuyPrice;
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Update the flag value of a profile inventory item
        /// </summary>
        /// <remarks>This API requires authentication</remarks>
        /// <param name="itemOrAssetId">
        /// Either a profile inventory item identifier, or asset description identifier. 
        /// If an asset description identifier is specified, all inventory items for that asset description will be updated (bulk update). 
        /// The item must belong to your (currently authenticated) profile
        /// </param>
        /// <param name="steamId">Valid SteamId (int64), ProfileId (string), or Steam profile page URL</param>
        /// <param name="flag">Name of a supported item flag. See <see cref="SteamProfileInventoryItemFlags"/></param>
        /// <param name="value">New value of the item flag</param>
        /// <response code="200">If the item flag was updated successfully.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="401">If the request is unauthenticated (login first) or the requested inventory item does not belong to the authenticated user.</response>
        /// <response code="404">If the inventory item cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(AuthorizationPolicies.User)]
        [HttpPut("{steamId}/inventory/item/{itemOrAssetId}/{flag}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SetInventoryItemFlag([FromRoute] string steamId, [FromRoute] string itemOrAssetId, [FromRoute] string flag, [FromBody] bool value)
        {
            SteamProfileInventoryItemFlags flagValue;
            if (!Enum.TryParse(flag, true, out flagValue))
            {
                return BadRequest("Unsupported inventory item flag name");
            }
            if (String.IsNullOrEmpty(itemOrAssetId))
            {
                return NotFound("Inventory item id is invalid");
            }

            var profileId = User.Id();
            var inventoryItems = await _db.SteamProfileInventoryItems
                .Where(x => x.ProfileId == profileId)
                .Where(x => x.SteamId == itemOrAssetId || x.Description.ClassId.ToString() == itemOrAssetId)
                .ToListAsync();
            if (!(inventoryItems?.Any() == true))
            {
                _logger.LogError($"No inventory item found that matches id '{itemOrAssetId}'");
                return NotFound($"No inventory item found that matches id '{itemOrAssetId}'");
            }

            foreach (var inventoryItem in inventoryItems)
            {
                if (value)
                {
                    inventoryItem.Flags |= flagValue;
                }
                else
                {
                    inventoryItem.Flags &= ~flagValue;
                }
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        #endregion

        #region Market Items

        /// <summary>
        /// Update the flag value of a profile market item
        /// </summary>
        /// <remarks>This API requires authentication</remarks>
        /// <param name="itemOrAssetId">
        /// Either a profile market item identifier, or asset description identifier. 
        /// If an asset description identifier is specified, all market items for that asset description will be updated (bulk update). 
        /// The item must belong to your (currently authenticated) profile
        /// </param>
        /// <param name="steamId">Valid SteamId (int64), ProfileId (string), or Steam profile page URL</param>
        /// <param name="flag">Name of a supported item flag. See <see cref="SteamProfileMarketItemFlags"/></param>
        /// <param name="value">New value of the item flag</param>
        /// <response code="200">If the item flag was updated successfully.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="401">If the request is unauthenticated (login first) or the requested market item does not belong to the authenticated user.</response>
        /// <response code="404">If the market item cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(AuthorizationPolicies.User)]
        [HttpPut("{steamId}/market/item/{itemOrAssetId}/{flag}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SetMarketItemFlag([FromRoute] string steamId, [FromRoute] string itemOrAssetId, [FromRoute] string flag, [FromBody] bool value)
        {
            SteamProfileMarketItemFlags flagValue;
            if (!Enum.TryParse(flag, true, out flagValue))
            {
                return BadRequest("Unsupported market item flag name");
            }
            if (String.IsNullOrEmpty(itemOrAssetId))
            {
                return NotFound("Market item id is invalid");
            }

            var profileId = User.Id();
            var marketItems = await _db.SteamProfileMarketItems
                .Where(x => x.ProfileId == profileId)
                .Where(x => x.SteamId == itemOrAssetId || x.Description.ClassId.ToString() == itemOrAssetId)
                .ToListAsync();

            if (!(marketItems?.Any() == true))
            {
                var assetDescription = await _db.SteamAssetDescriptions
                    .Where(x => x.ClassId.ToString() == itemOrAssetId)
                    .FirstOrDefaultAsync();
                if (assetDescription == null)
                {
                    _logger.LogError($"No market item found that matches id '{itemOrAssetId}'");
                    return NotFound($"No market item found that matches id '{itemOrAssetId}'");
                }

                var marketItem = new SteamProfileMarketItem()
                {
                    SteamId = assetDescription.NameId?.ToString(),
                    AppId = assetDescription.AppId,
                    DescriptionId = assetDescription.Id,
                    ProfileId = profileId,
                    Flags = flagValue
                };

                _db.SteamProfileMarketItems.Add(marketItem);
            }
            else
            {
                foreach (var marketItem in marketItems)
                {
                    if (value)
                    {
                        marketItem.Flags |= flagValue;
                    }
                    else
                    {
                        marketItem.Flags &= ~flagValue;
                        if (marketItem.Flags == SteamProfileMarketItemFlags.None)
                        {
                            _db.SteamProfileMarketItems.Remove(marketItem);
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        #endregion

        /// <summary>
        /// List all users who have donated
        /// </summary>
        /// <returns>The list of users who have donated</returns>
        /// <response code="200">The list of users who have donated.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("donators")]
        [ProducesResponseType(typeof(IEnumerable<ProfileDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDonators()
        {
            var donators = await _db.SteamProfiles
                .AsNoTracking()
                .Where(x => x.DonatorLevel > 0)
                .OrderByDescending(x => x.DonatorLevel)
                .Select(x => _mapper.Map<ProfileDTO>(x))
                .ToListAsync();

            return Ok(donators);
        }
    }
}
