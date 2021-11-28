using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Extensions;
using SCMM.Shared.Web.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models;
using SCMM.Web.Data.Models.Extensions;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Data.Models.UI.Profile;
using SCMM.Web.Data.Models.UI.Profile.Inventory;
using SCMM.Web.Server.Extensions;
using System.Text.Json;

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
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <response code="200">If the session is authentication, your Steam profile information is returned. If the session is unauthenticated, a generic guest profile is returned.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(MyProfileDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyProfile()
        {
            var defaultProfile = _mapper.Map<SteamProfile, MyProfileDTO>(new SteamProfile(), this);
            defaultProfile.Name = "Guest";
            defaultProfile.Language = this.Language();
            defaultProfile.Currency = this.Currency();

            // If the user is authenticated, use their database profile
            if (User.Identity.IsAuthenticated)
            {
                var profileId = User.Id();
                var profile = await _db.SteamProfiles
                    .AsNoTracking()
                    .Include(x => x.Language)
                    .Include(x => x.Currency)
                    .FirstOrDefaultAsync(x => x.Id == profileId);

                return Ok(
                    _mapper.Map<SteamProfile, MyProfileDTO>(profile, this) ?? defaultProfile
                );
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
                return BadRequest($"No data to update");
            }

            var profileId = User.Id();
            var profile = await _db.SteamProfiles
                .Include(x => x.Language)
                .Include(x => x.Currency)
                .FirstOrDefaultAsync(x => x.Id == profileId);

            if (profile == null)
            {
                return BadRequest($"Profile not found");
            }

            // General
            if (command.TradeUrl != null)
            {
                profile.TradeUrl = command.TradeUrl;
            }
            profile.ItemAnalyticsParticipation = command.ItemAnalyticsParticipation;
            profile.GamblingOffset = command.GamblingOffset;

            // Preferences
            if (command.Language != null)
            {
                profile.Language = _db.SteamLanguages.FirstOrDefault(x => x.Name == command.Language);
            }
            if (command.Currency != null)
            {
                profile.Currency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == command.Currency);
            }
            profile.StoreTopSellers = command.StoreTopSellers;
            profile.MarketValue = command.MarketValue;
            profile.IncludeMarketTax = command.IncludeMarketTax;
            profile.ItemInfo = command.ItemInfo;
            profile.ItemInfoWebsite = command.ItemInfoWebsite;
            profile.ShowItemDrops = command.ShowItemDrops;

            // Notifications
            if (command.DiscordId != null)
            {
                profile.DiscordId = command.DiscordId;
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Download a copy of all data related to your profile
        /// </summary>
        /// <remarks>This API requires authentication</remarks>
        /// <response code="200">Steam profile data.</response>
        /// <response code="400">If profile cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(AuthorizationPolicies.User)]
        [HttpGet("data")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyProfileData()
        {
            var profileId = User.Id();
            var profile = await _db.SteamProfiles
                .Include(x => x.InventoryItems)
                .Include(x => x.MarketItems)
                .Include(x => x.AssetDescriptions)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == profileId);

            if (profile == null)
            {
                return BadRequest($"Profile not found");
            }

            // Null out all loopback properties to prevent cyclic references when serialising
            foreach (var item in profile.InventoryItems)
            {
                item.Profile = null;
            }
            foreach (var item in profile.MarketItems)
            {
                item.Profile = null;
            }
            foreach (var item in profile.AssetDescriptions)
            {
                item.CreatorProfile = null;
            }

            return File(
                JsonSerializer.SerializeToUtf8Bytes(profile),
                "text/json",
                $"{profileId}.{DateTime.UtcNow.Ticks}.data.json"
            );
        }

        /// <summary>
        /// Delete all data related to your profile
        /// </summary>
        /// <remarks>This API requires authentication</remarks>
        /// <response code="200">If the deletion was successful.</response>
        /// <response code="400">If the profile cannot be deleted.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(AuthorizationPolicies.User)]
        [HttpDelete("data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteMyProfileData()
        {
            var profileId = User.Id();
            var profile = await _db.SteamProfiles
                .Include(x => x.InventoryItems)
                .Include(x => x.MarketItems)
                .Include(x => x.AssetDescriptions)
                .FirstOrDefaultAsync(x => x.Id == profileId);

            if (profile == null)
            {
                return BadRequest($"Profile not found");
            }

            // Does this profile own accepted asset descriptions?
            if (profile.AssetDescriptions.Where(x => x.IsAccepted).Any())
            {
                // Strip the profile back to the minimum, but don't delete it
                profile.RemoveNonEssentialData();
            }
            else
            {
                // Delete the profile
                _db.SteamProfiles.Remove(profile);
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Get profile information
        /// </summary>
        /// <param name="id">Valid Steam ID64, Custom URL, or Profile URL</param>
        /// <response code="200">Steam profile information.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="404">If the profile cannot be found or the profile is private.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/summary")]
        [ProducesResponseType(typeof(ProfileDetailedDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfileSummary([FromRoute] string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID is invalid");
            }

            // Load the profile
            var importedProfile = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
            {
                ProfileId = id
            });

            var profile = importedProfile?.Profile;
            if (profile == null)
            {
                return NotFound($"Profile not found");
            }

            await _db.SaveChangesAsync();
            return Ok(
                _mapper.Map<SteamProfile, ProfileDetailedDTO>(
                    profile, this
                )
            );
        }

        /// <summary>
        /// Synchronise Steam profile inventory
        /// </summary>
        /// <param name="id">Valid Steam ID64, Custom URL, or Profile URL</param>
        /// <param name="force">If true, the inventory will always be fetched from Steam. If false, sync calls to Steam are cached for one hour</param>
        /// <response code="200">If the inventory was successfully synchronised.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="401">If the profile inventory is private.</response>
        /// <response code="404">If the profile cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpPost("{id}/inventory/sync")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostInventorySync([FromRoute] string id, [FromQuery] bool force = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID is invalid");
            }

            // Reload the profile's inventory
            var importedInventory = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileInventoryRequest()
            {
                ProfileId = id,
                Force = force
            });

            var profile = importedInventory?.Profile;
            if (profile == null)
            {
                return NotFound($"Profile not found");
            }
            if (profile?.Privacy != SteamVisibilityType.Public)
            {
                return Unauthorized($"Profile inventory is private");
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Get Steam profile inventory and calculate the market value
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// Inventory mosaic images automatically expire after 7 days; After which, the URL will return a 404 response.
        /// </remarks>
        /// <param name="id">Valid Steam ID64, Custom URL, or Profile URL</param>
        /// <param name="generateInventoryMosaic">If true, a mosaic image of the highest valued items will be generated in the response. If false, the mosaic will be <code>null</code>.</param>
        /// <param name="mosaicTileSize">The size (in pixel) to render each item within the mosaic image (if enabled)</param>
        /// <param name="mosaicColumns">The number of item columns to render within the mosaic image (if enabled)</param>
        /// <param name="mosaicRows">The number of item rows to render within the mosaic image (if enabled)</param>
        /// <param name="force">If true, the inventory will always be fetched from Steam. If false, calls to Steam are cached for one hour</param>
        /// <response code="200">Profile inventory value.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="401">If the profile inventory is private/empty.</response>
        /// <response code="404">If the profile cannot be found or the inventory contains no (marketable) items.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/inventory/value")]
        [ProducesResponseType(typeof(ProfileInventoryValueDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInventoryValue([FromRoute] string id, [FromQuery] bool generateInventoryMosaic = false, [FromQuery] int mosaicTileSize = 128, [FromQuery] int mosaicColumns = 5, [FromQuery] int mosaicRows = 5, [FromQuery] bool force = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID is invalid");
            }

            // Load the profile
            var importedProfile = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
            {
                ProfileId = id
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
                return Unauthorized("Profile inventory is private");
            }

            await _db.SaveChangesAsync();

            // Calculate the profiles inventory totals
            var inventoryTotals = await _queryProcessor.ProcessAsync(new GetSteamProfileInventoryTotalsRequest()
            {
                ProfileId = profile.SteamId,
                CurrencyId = this.Currency().Id.ToString()
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
                    InventoryMosaicUrl = inventoryThumbnail?.Image?.Id != null ? $"{_configuration.GetWebsiteUrl()}/api/image/{inventoryThumbnail.Image.Id}" : null,
                    Items = inventoryTotals.Items,
                    Invested = inventoryTotals.Invested,
                    InvestmentGains = inventoryTotals.InvestmentGains,
                    InvestmentLosses = inventoryTotals.InvestmentLosses,
                    MarketValue = inventoryTotals.MarketValue,
                    MarketMovementValue = inventoryTotals.MarketMovementValue,
                    MarketMovementTime = inventoryTotals.MarketMovementTime,
                }
            );
        }

        /// <summary>
        /// Get profile inventory item totals
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="id">Valid Steam ID64, Custom URL, or Profile URL</param>
        /// <response code="200">Profile inventory item totals.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="404">If the profile cannot be found or if the inventory is private/empty.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/inventory/total")]
        [ProducesResponseType(typeof(ProfileInventoryTotalsDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInventoryTotal([FromRoute] string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID is invalid");
            }

            var inventoryTotals = await _queryProcessor.ProcessAsync(new GetSteamProfileInventoryTotalsRequest()
            {
                ProfileId = id,
                CurrencyId = this.Currency().Id.ToString()
            });
            if (inventoryTotals == null)
            {
                return NotFound("Profile inventory is empty (private, or no marketable items)");
            }

            return Ok(
                _mapper.Map<GetSteamProfileInventoryTotalsResponse, ProfileInventoryTotalsDTO>(inventoryTotals, this)
            );
        }

        /// <summary>
        /// Get profile inventory item information
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="id">Valid Steam ID64, Custom URL, or Profile URL</param>
        /// <response code="200">Profile inventory item information.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/inventory/items")]
        [ProducesResponseType(typeof(IList<ProfileInventoryItemDescriptionDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInventoryItems([FromRoute] string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID is invalid");
            }

            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = id
            });
            if (resolvedId?.Exists != true)
            {
                return NotFound("Profile not found");
            }

            var showDrops = this.User.Preference(_db, x => x.ShowItemDrops);
            var profileInventoryItems = await _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => x.Description != null)
                .Where(x => showDrops || (!x.Description.IsSpecialDrop && !x.Description.IsTwitchDrop))
                .Include(x => x.Description)
                .Include(x => x.Description.App)
                .Include(x => x.Description.StoreItem)
                .Include(x => x.Description.StoreItem.Currency)
                .Include(x => x.Description.MarketItem)
                .Include(x => x.Description.MarketItem.Currency)
                .ToListAsync();

            var profileInventoryItemsSummaries = new List<ProfileInventoryItemDescriptionDTO>();
            foreach (var item in profileInventoryItems)
            {
                if (!profileInventoryItemsSummaries.Any(x => x.Id == item.Description.ClassId))
                {
                    var itemSummary = _mapper.Map<SteamAssetDescription, ProfileInventoryItemDescriptionDTO>(
                        item.Description, this
                    );

                    // Calculate the item's quantity
                    itemSummary.Quantity = profileInventoryItems
                        .Where(x => x.Description.ClassId == item.Description.ClassId)
                        .Sum(x => x.Quantity);

                    profileInventoryItemsSummaries.Add(itemSummary);
                }
            }

            return Ok(
                profileInventoryItemsSummaries.OrderByDescending(x => x.BuyNowPrice)
            );
        }

        /// <summary>
        /// Get profile inventory item collection information
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="id">Valid Steam ID64, Custom URL, or Profile URL</param>
        /// <response code="200">Profile inventory item collection information.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/inventory/collections")]
        [ProducesResponseType(typeof(IEnumerable<ProfileInventoryCollectionDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInventoryCollections([FromRoute] string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID is invalid");
            }

            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = id
            });
            if (resolvedId?.Exists != true)
            {
                return NotFound("Profile not found");
            }

            var profileItemsInCollection = await _db.SteamProfileInventoryItems
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => !String.IsNullOrEmpty(x.Description.ItemCollection))
                .Select(x => new
                {
                    ClassId = x.Description.ClassId,
                    CreatorId = x.Description.CreatorId,
                    ItemCollection = x.Description.ItemCollection
                })
                .ToListAsync();
            if (profileItemsInCollection?.Any() != true)
            {
                return NotFound("No item collections found");
            }

            var showDrops = this.User.Preference(_db, x => x.ShowItemDrops);
            var itemCollections = profileItemsInCollection.Select(x => x.ItemCollection).Distinct().ToArray();
            var profileCollections = await _db.SteamAssetDescriptions.AsNoTracking()
                .Where(x => itemCollections.Contains(x.ItemCollection))
                .Where(x => showDrops || (!x.IsSpecialDrop && !x.IsTwitchDrop))
                .Include(x => x.App)
                .Include(x => x.CreatorProfile)
                .ToListAsync();

            var profileCollectionsGrouped = profileCollections
                .GroupBy(x => new
                {
                    Collection = x.ItemCollection,
                    CreatorName = (!x.IsSpecialDrop && !x.IsTwitchDrop) ? x.CreatorProfile?.Name : null,
                    CreatorAvatarUrl = (!x.IsSpecialDrop && !x.IsTwitchDrop) ? x.CreatorProfile?.AvatarUrl : null
                })
                .Select(x => new ProfileInventoryCollectionDTO()
                {
                    Name = x.Key.Collection,
                    CreatorName = x.Key.CreatorName,
                    CreatorAvatarUrl = x.Key.CreatorAvatarUrl,
                    Items = x.Select(y => new ProfileInventoryCollectionItemDTO()
                    {
                        Item = _mapper.Map<SteamAssetDescription, ItemDescriptionDTO>(y, this),
                        IsOwned = profileItemsInCollection.Any(z => z.ClassId == y.ClassId)
                    }).ToList()
                })
                .Where(x => x.Items.Any(y => y.IsOwned))
                .OrderByDescending(x => x.Items.Where(x => x.IsOwned).Count())
                .ToList();

            return Ok(
                profileCollectionsGrouped
            );
        }

        /// <summary>
        /// Get profile inventory market movement
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="id">Valid Steam ID64, Custom URL, or Profile URL</param>
        /// <response code="200">Profile inventory market movement information.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/inventory/movement")]
        [ProducesResponseType(typeof(IEnumerable<ProfileInventoryItemMovementDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInventoryMovement([FromRoute] string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID is invalid");
            }

            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = id
            });
            if (resolvedId?.Exists != true)
            {
                return NotFound("Profile not found");
            }

            var dayOpenTimestamp = new DateTimeOffset(DateTime.UtcNow.Date, TimeZoneInfo.Utc.BaseUtcOffset);
            var profileItemMovements = await _db.SteamProfileInventoryItems
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => x.Description != null && x.Description.MarketItem != null)
                .Where(x => !x.Description.IsSpecialDrop && !x.Description.IsTwitchDrop)
                .Select(x => new
                {
                    App = x.App,
                    Description = x.Description,
                    CurrentValue = (x.Description.MarketItem.SellOrderLowestPrice > 0 ? ((decimal)x.Description.MarketItem.SellOrderLowestPrice / x.Description.MarketItem.Currency.ExchangeRateMultiplier) : 0),
                    Stable24hrValue = (x.Description.MarketItem.Stable24hrSellOrderLowestPrice > 0 ? ((decimal)x.Description.MarketItem.Stable24hrSellOrderLowestPrice / x.Description.MarketItem.Currency.ExchangeRateMultiplier) : 0),
                    Quantity = x.Quantity
                })
                .ToListAsync();
            if (profileItemMovements?.Any() != true)
            {
                return NotFound("No marketable items found");
            }

            var groupedItemMovement = profileItemMovements
                .GroupBy(x => x.Description)
                .Select(x => new ProfileInventoryItemMovementDTO
                {
                    Item = _mapper.Map<SteamAssetDescription, ItemDescriptionDTO>(x.Key, this),
                    MovementTime = dayOpenTimestamp,
                    Movement = x.Any() ? (this.Currency().CalculateExchange(x.FirstOrDefault()?.CurrentValue ?? 0m) - this.Currency().CalculateExchange(x.FirstOrDefault()?.Stable24hrValue ?? 0m)) : 0,
                    Value = this.Currency().CalculateExchange(x.FirstOrDefault()?.CurrentValue ?? 0m),
                    Quantity = x.Sum(y => y.Quantity)
                })
                .Where(x => x.Movement != 0)
                .OrderByDescending(x => x.Movement)
                .ToList();

            return Ok(groupedItemMovement);
        }

        /// <summary>
        /// Get profile inventory investment information
        /// </summary>
        /// <remarks>
        /// This API requires authentication.
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="id">Valid Steam ID64, Custom URL, or Profile URL</param>
        /// <param name="filter">Optional search filter. Matches against item name or description</param>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <param name="sortBy">Sort item property name from <see cref="ProfileInventoryInvestmentItemDTO"/></param>
        /// <param name="sortDirection">Sort item direction</param>
        /// <response code="200">Profile inventory investment information.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="401">If the request is unauthenticated (login first) or the requested inventory does not belong to the authenticated user.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(AuthorizationPolicies.User)]
        [HttpGet("{id}/inventory/investment")]
        [ProducesResponseType(typeof(PaginatedResult<ProfileInventoryInvestmentItemDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInventoryInvestment([FromRoute] string id, [FromQuery] string filter = null, [FromQuery] int start = 0, [FromQuery] int count = 10, [FromQuery] string sortBy = null, [FromQuery] SortDirection sortDirection = SortDirection.Ascending)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID is invalid");
            }

            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = id
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

            filter = Uri.UnescapeDataString(filter?.Trim() ?? string.Empty);
            var query = _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => x.Description != null)
                .Where(x => !x.Description.IsSpecialDrop && !x.Description.IsTwitchDrop)
                .Where(x => string.IsNullOrEmpty(filter) || x.Description.Name.ToLower().Contains(filter.ToLower()))
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description.MarketItem)
                .Include(x => x.Description.MarketItem.Currency)
                .Include(x => x.Description.StoreItem)
                .Include(x => x.Description.StoreItem.Currency)
                .AsQueryable();

            switch (sortBy)
            {
                case nameof(ProfileInventoryInvestmentItemDTO.Name):
                    query = query.OrderBy(x => x.Description.Name, sortDirection);
                    break;
                case nameof(ProfileInventoryInvestmentItemDTO.BuyPrice):
                    query = query.OrderBy(x => x.BuyPrice, sortDirection);
                    break;
                case nameof(ProfileInventoryInvestmentItemDTO.ResellPrice):
                    query = query.OrderBy(x => x.Description.MarketItem.ResellPrice, sortDirection);
                    break;
                case nameof(ProfileInventoryInvestmentItemDTO.ResellTax):
                    query = query.OrderBy(x => x.Description.MarketItem.ResellTax, sortDirection);
                    break;
                case "ResellProfit":
                    query = query.OrderBy(x =>
                        ((x.Description.MarketItem.ResellPrice - x.Description.MarketItem.ResellTax) != 0 && x.BuyPrice > 0 && x.Currency != null)
                            ? ((x.Description.MarketItem.ResellPrice - x.Description.MarketItem.ResellTax) / x.Description.MarketItem.Currency.ExchangeRateMultiplier) - (x.BuyPrice / x.Currency.ExchangeRateMultiplier)
                            : 0
                        , sortDirection);
                    break;
                case "ResellRoI":
                    query = query.OrderBy(x =>
                        ((x.Description.MarketItem.ResellPrice - x.Description.MarketItem.ResellTax) != 0 && x.BuyPrice > 0 && x.Currency != null)
                            ? ((x.Description.MarketItem.ResellPrice - x.Description.MarketItem.ResellTax) / x.Description.MarketItem.Currency.ExchangeRateMultiplier) / (x.BuyPrice / x.Currency.ExchangeRateMultiplier)
                            : 0
                        , sortDirection);
                    break;
            }

            var results = await query.PaginateAsync(start, count,
                x => _mapper.Map<SteamProfileInventoryItem, ProfileInventoryInvestmentItemDTO>(x, this)
            );

            return Ok(results);
        }

        /// <summary>
        /// Update profile inventory item information
        /// </summary>
        /// <remarks>This API requires authentication</remarks>
        /// <param name="id">Valid Steam ID64, Custom URL, or Profile URL</param>
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
        [HttpPut("{id}/inventory/item/{itemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SetInventoryItem([FromRoute] string id, [FromRoute] Guid itemId, [FromBody] UpdateInventoryItemCommand command)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID is invalid");
            }
            if (itemId == Guid.Empty)
            {
                return BadRequest("Inventory item GUID is invalid");
            }
            if (command == null)
            {
                return BadRequest($"No data to update");
            }

            var inventoryItem = await _db.SteamProfileInventoryItems.FirstOrDefaultAsync(x => x.Id == itemId);
            if (inventoryItem == null)
            {
                _logger.LogError($"Inventory item was not found");
                return NotFound($"Inventory item  was not found");
            }
            if (!User.Is(inventoryItem.ProfileId) && !User.IsInRole(Roles.Administrator))
            {
                _logger.LogError($"Inventory item does not belong to you and you do not have permission to modify it");
                return Unauthorized($"Inventory item does not belong to you and you do not have permission to modify it");
            }

            if (command.BuyPrice != null)
            {
                inventoryItem.BuyPrice = (command.BuyPrice > 0 ? command.BuyPrice : null);
            }
            if (command.CurrencyGuid != null)
            {
                inventoryItem.CurrencyId = command.CurrencyGuid;
            }
            if (command.AcquiredBy != null)
            {
                inventoryItem.AcquiredBy = command.AcquiredBy.Value;
                switch (inventoryItem.AcquiredBy)
                {
                    // Items sourced from gambling, gifts, and drops don't need prices
                    case SteamProfileInventoryItemAcquisitionType.Gambling:
                    case SteamProfileInventoryItemAcquisitionType.Gift:
                    case SteamProfileInventoryItemAcquisitionType.Drop:
                        {
                            inventoryItem.CurrencyId = null;
                            inventoryItem.BuyPrice = null;
                            break;
                        }
                }
            }

            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
