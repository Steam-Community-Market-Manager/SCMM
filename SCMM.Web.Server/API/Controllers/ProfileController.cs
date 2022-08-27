using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Json;
using SCMM.Shared.Data.Store.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models;
using SCMM.Web.Data.Models.Extensions;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Data.Models.UI.Profile;
using SCMM.Web.Data.Models.UI.Profile.Inventory;
using SCMM.Web.Server.Extensions;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            defaultProfile.App = this.App();

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
            profile.ItemInfo = command.ItemInfo;
            profile.ItemInfoWebsite = command.ItemInfoWebsite;
            profile.ItemIncludeMarketFees = command.ItemIncludeMarketFees;
            profile.InventoryShowItemDrops = command.InventoryShowItemDrops;
            profile.InventoryShowUnmarketableItems = command.InventoryShowUnmarketableItems;
            profile.InventoryValueMovementDisplay = command.InventoryValueMovemenDisplay;

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

            var serialiserOptions = new JsonSerializerOptions().UseDefaults();
            serialiserOptions.WriteIndented = true;

            return File(
                JsonSerializer.SerializeToUtf8Bytes(profile, serialiserOptions),
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
        /// <response code="404">If the profile cannot be found.</response>
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

            await _db.SaveChangesAsync();

            var profile = importedProfile?.Profile;
            if (profile == null)
            {
                return NotFound($"Profile not found");
            }

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
                AppId = this.App().Id.ToString(),
                Force = force
            });

            await _db.SaveChangesAsync();

            var profile = importedInventory?.Profile;
            if (profile == null)
            {
                return NotFound($"Profile not found");
            }
            if (profile?.Privacy != SteamVisibilityType.Public)
            {
                return Unauthorized($"Profile inventory is private");
            }

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
        /// <param name="mosaicTileSize">The size (in pixel) to render each item within the mosaic image (if enabled) (range: 32-128)</param>
        /// <param name="mosaicColumns">The number of item columns to render within the mosaic image (if enabled) (range: 1-10)</param>
        /// <param name="mosaicRows">The number of item rows to render within the mosaic image (if enabled) (range: 1-10)</param>
        /// <param name="force">If true, the inventory will always be fetched from Steam. If false, calls to Steam are cached for one hour</param>
        /// <response code="200">Profile inventory value.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="401">If the profile inventory is private/empty.</response>
        /// <response code="404">If the profile cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/inventory/value")]
        [ProducesResponseType(typeof(ProfileInventoryValueDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInventoryValue([FromRoute] string id, [FromQuery] bool generateInventoryMosaic = false, [FromQuery] int mosaicTileSize = 64, [FromQuery] int mosaicColumns = 5, [FromQuery] int mosaicRows = 5, [FromQuery] bool force = false)
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

            await _db.SaveChangesAsync();

            var profile = importedProfile?.Profile;
            if (profile == null)
            {
                return NotFound("Profile not found");
            }

            // Reload the profiles inventory
            var importedInventory = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileInventoryRequest()
            {
                ProfileId = profile.Id.ToString(),
                AppId = this.App().Id.ToString(),
                Force = force
            });

            await _db.SaveChangesAsync();

            if (importedInventory?.Profile?.Privacy != SteamVisibilityType.Public)
            {
                return Unauthorized("Profile inventory is private");
            }

            // Calculate the profiles inventory totals
            var inventoryTotals = await _commandProcessor.ProcessWithResultAsync(new CalculateSteamProfileInventoryTotalsRequest()
            {
                ProfileId = profile.SteamId,
                AppId = this.App().Id.ToString(),
                CurrencyId = this.Currency().Id.ToString()
            });

            await _db.SaveChangesAsync();

            if (inventoryTotals == null)
            {
                return NotFound("Profile inventory data is missing");
            }

            // Generate the profiles inventory thumbnail
            var inventoryThumbnailImageUrl = (string)null;
            try
            {
                inventoryThumbnailImageUrl = (
                    await _commandProcessor.ProcessWithResultAsync(new GenerateSteamProfileInventoryThumbnailRequest()
                    {
                        ProfileId = profile.SteamId,
                        ItemSize = Math.Max(32, Math.Min(mosaicTileSize, 128)),
                        ItemColumns = Math.Max(1, Math.Min(mosaicColumns, 10)),
                        ItemRows = Math.Max(1, Math.Min(mosaicRows, 10)),
                        ExpiresOn = DateTimeOffset.Now.AddDays(7)
                    })
                )?.ImageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to generate profile inventory thumbnail");
            }

            await _db.SaveChangesAsync();

            return Ok(
                new ProfileInventoryValueDTO()
                {
                    SteamId = profile.SteamId,
                    Name = profile.Name,
                    AvatarUrl = profile.AvatarUrl,
                    InventoryMosaicUrl = inventoryThumbnailImageUrl,
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
        /// <response code="404">If the profile cannot be found.</response>
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

            var inventoryTotals = await _commandProcessor.ProcessWithResultAsync(new CalculateSteamProfileInventoryTotalsRequest()
            {
                ProfileId = id,
                AppId = this.App().Id.ToString(),
                CurrencyId = this.Currency().Id.ToString()
            });

            await _db.SaveChangesAsync();

            return Ok(
                _mapper.Map<CalculateSteamProfileInventoryTotalsResponse, ProfileInventoryTotalsDTO>(inventoryTotals, this)
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
        /// <response code="404">If the profile cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/inventory/items")]
        [ProducesResponseType(typeof(IList<ProfileInventoryItemDescriptionDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

            var app = this.App();
            var isMyInventory = (User.SteamId() == resolvedId.Profile?.SteamId);
            var showDrops = this.User.Preference(_db, x => x.InventoryShowItemDrops);
            var showUnmarketable = this.User.Preference(_db, x => x.InventoryShowUnmarketableItems);
            var profileInventoryItems = await _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => x.AppId == app.Guid)
                .Where(x => x.Description != null)
                .Where(x => showDrops || (!x.Description.IsSpecialDrop && !x.Description.IsTwitchDrop))
                .Where(x => showUnmarketable || (x.Description.IsMarketable || x.Description.MarketableRestrictionDays > 0))
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

                    var itemInventoryInstances = profileInventoryItems
                        .Where(x => x.Description.ClassId == item.Description.ClassId);

                    // Calculate the item's quantity and average price
                    itemSummary.Quantity = itemInventoryInstances.Sum(x => x.Quantity);
                    itemSummary.AverageBuyPrice = (isMyInventory && itemInventoryInstances.Any(x => x.BuyPrice > 0))
                        ? (long) Math.Round(itemInventoryInstances.Where(x => x.BuyPrice > 0).Average(x => x.BuyPrice.Value), 0) 
                        : 0;

                    // Calculate the item's stack sizes
                    itemSummary.Stacks = _mapper.Map<SteamProfileInventoryItem, ProfileInventoryItemDescriptionStackDTO>(
                        itemInventoryInstances, this
                    )?.ToArray();

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
        /// <response code="404">If the profile cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/inventory/collections")]
        [ProducesResponseType(typeof(IEnumerable<ProfileInventoryCollectionDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

            var app = this.App();
            var profileItemsInCollection = await _db.SteamProfileInventoryItems
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => x.AppId == app.Guid)
                .Where(x => !String.IsNullOrEmpty(x.Description.ItemCollection))
                .Select(x => new
                {
                    ClassId = x.Description.ClassId,
                    CreatorId = x.Description.CreatorId,
                    ItemCollection = x.Description.ItemCollection
                })
                .ToListAsync();
            
            var showDrops = this.User.Preference(_db, x => x.InventoryShowItemDrops);
            var showUnmarketable = this.User.Preference(_db, x => x.InventoryShowUnmarketableItems);
            var itemCollections = profileItemsInCollection.Select(x => x.ItemCollection).Distinct().ToArray();
            var profileCollections = await _db.SteamAssetDescriptions.AsNoTracking()
                .Where(x => x.AppId == app.Guid)
                .Where(x => itemCollections.Contains(x.ItemCollection))
                .Where(x => showDrops || (!x.IsSpecialDrop && !x.IsTwitchDrop))
                .Where(x => showUnmarketable || (x.IsMarketable || x.MarketableRestrictionDays > 0))
                .Include(x => x.App)
                .Include(x => x.CreatorProfile)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
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
                        Item = _mapper.Map<SteamAssetDescription, ItemDescriptionWithPriceDTO>(y, this),
                        IsOwned = profileItemsInCollection.Any(z => z.ClassId == y.ClassId)
                    }).ToArray()
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
        /// <response code="404">If the profile cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/inventory/movement")]
        [ProducesResponseType(typeof(IEnumerable<ProfileInventoryItemMovementDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

            var app = this.App();
            var dayOpenTimestamp = new DateTimeOffset(DateTime.UtcNow.Date, TimeZoneInfo.Utc.BaseUtcOffset);
            var profileItemMovements = await _db.SteamProfileInventoryItems
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => x.AppId == app.Guid)
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
        /// <response code="404">If the profile cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(AuthorizationPolicies.User)]
        [HttpGet("{id}/inventory/investment")]
        [ProducesResponseType(typeof(PaginatedResult<ProfileInventoryInvestmentItemDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
            if (resolvedId?.Exists != true || resolvedId.ProfileId == null || resolvedId.Profile == null)
            {
                return NotFound("Profile not found");
            }

            if (!User.Is(resolvedId.ProfileId.Value) && !User.IsInRole(Roles.Administrator))
            {
                _logger.LogError($"Inventory does not belong to you and you do not have permission to view it");
                return Unauthorized($"Inventory does not belong to you and you do not have permission to view it");
            }

            filter = Uri.UnescapeDataString(filter?.Trim() ?? string.Empty);
            var app = this.App();
            var query = _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => x.AppId == app.Guid)
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

            var includeFees = resolvedId.Profile.ItemIncludeMarketFees;
            switch (sortBy)
            {
                case nameof(ProfileInventoryInvestmentItemDTO.Name):
                    query = query.SortBy(x => x.Description.Name, sortDirection);
                    break;
                case nameof(ProfileInventoryInvestmentItemDTO.BuyPrice):
                    query = query.SortBy(x => x.BuyPrice, sortDirection);
                    break;
                case nameof(ProfileInventoryInvestmentItemDTO.SellLaterPrice):
                    query = query.SortBy(x => x.Description.MarketItem.SellLaterPrice, sortDirection);
                    break;
                case nameof(ProfileInventoryInvestmentItemDTO.SellLaterFee):
                    query = query.SortBy(x => (includeFees ? x.Description.MarketItem.SellLaterFee : 0), sortDirection);
                    break;
                case "SellLaterProfit":
                    query = query.SortBy(x =>
                        ((x.Description.MarketItem.SellLaterPrice - (includeFees ? x.Description.MarketItem.SellLaterFee : 0)) != 0 && x.BuyPrice > 0 && x.Currency != null)
                            ? ((x.Description.MarketItem.SellLaterPrice - (includeFees ? x.Description.MarketItem.SellLaterFee : 0)) / x.Description.MarketItem.Currency.ExchangeRateMultiplier) - (x.BuyPrice / x.Currency.ExchangeRateMultiplier)
                            : 0
                        , sortDirection);
                    break;
                case "SellLaterRoI":
                    query = query.SortBy(x =>
                        ((x.Description.MarketItem.SellLaterPrice - (includeFees ? x.Description.MarketItem.SellLaterFee : 0)) != 0 && x.BuyPrice > 0 && x.Currency != null)
                            ? ((x.Description.MarketItem.SellLaterPrice - (includeFees ? x.Description.MarketItem.SellLaterFee : 0)) / x.Description.MarketItem.Currency.ExchangeRateMultiplier) / (x.BuyPrice / x.Currency.ExchangeRateMultiplier)
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
        /// <param name="profileId">Valid Steam ID64, Custom URL, or Profile URL</param>
        /// <param name="itemId">
        /// Inventory item identifier (Steam ID64) to be updated. 
        /// The item must belong to your (currently authenticated) profile or the request will fail
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
        [HttpPut("{profileId}/inventory/item/{itemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SetInventoryItem([FromRoute] string profileId, [FromRoute] ulong itemId, [FromBody] UpdateInventoryItemCommand command)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                return BadRequest("Profile id is invalid");
            }
            if (itemId <= 0)
            {
                return BadRequest("Item id is invalid");
            }
            if (command == null)
            {
                return BadRequest($"Item command is invalid, no data to update");
            }

            var inventoryItem = await _db.SteamProfileInventoryItems.FirstOrDefaultAsync(x => x.SteamId == itemId.ToString());
            if (inventoryItem == null)
            {
                _logger.LogError($"Item was not found");
                return NotFound($"Item  was not found");
            }
            if (!User.Is(inventoryItem.ProfileId) && !User.IsInRole(Roles.Administrator))
            {
                _logger.LogError($"Item does not belong to you and you do not have permission to modify it");
                return Unauthorized($"Item does not belong to you and you do not have permission to modify it");
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

        /// <summary>
        /// Combine multiple inventory item stacks together
        /// </summary>
        /// <remarks>This API requires authentication and a Steam Web API key to use</remarks>
        /// <param name="profileId">Valid Steam ID64, Custom URL, or Profile URL</param>
        /// <param name="itemId">
        /// The destination item id that will receive all source items. 
        /// The item must belong to your (currently authenticated) profile or the request will fail
        /// </param>
        /// <param name="sourceItems">
        /// A dictionary of item ids (keys) and their quantities (values) that will be combined in to the destination item
        /// </param>
        /// <param name="apiKey">
        /// Valid Steam Web API key with permission to modify the source and destination items.
        /// You can obtain your Steam API key from: https://steamcommunity.com/dev/apikey.
        /// Read https://scmm.app/privacy for more about how your Steam API key is handled.
        /// </param>
        /// <response code="200">If the inventory items were combined successfully.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="401">If the request is unauthenticated (login first), Steam API key is invalid, or the requested inventory items do not belong to the authenticated user.</response>
        /// <response code="404">If the inventory items cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(AuthorizationPolicies.User)]
        [HttpPut("{profileId}/inventory/item/{itemId}/combine")]
        [ProducesResponseType(typeof(ProfileInventoryItemDescriptionStackDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CombineInventoryItemStacks([FromHeader(Name = "Steam-Api-Key")] string apiKey, [FromRoute] string profileId, [FromRoute] ulong itemId, [FromBody] IDictionary<ulong, uint> sourceItems)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                return BadRequest("Profile id is invalid");
            }
            if (itemId <= 0)
            {
                return BadRequest("Item id is invalid");
            }
            if (sourceItems == null || !sourceItems.Any())
            {
                return BadRequest($"Source items are invalid, must specify at least one other item");
            }

            var destinationItem = await _db.SteamProfileInventoryItems.FirstOrDefaultAsync(x => x.SteamId == itemId.ToString());
            if (destinationItem == null)
            {
                _logger.LogError($"Item was not found");
                return NotFound($"Item  was not found");
            }
            if (!User.Is(destinationItem.ProfileId))
            {
                _logger.LogError($"Item does not belong to you and you do not have permission to modify it");
                return Unauthorized($"Item does not belong to you and you do not have permission to modify it");
            }

            try
            {
                var result = await _commandProcessor.ProcessWithResultAsync(new CombineInventoryItemStacksRequest()
                {
                    ProfileId = profileId,
                    ApiKey = apiKey,
                    SourceItems = sourceItems,
                    DestinationItemId = itemId
                });

                await _db.SaveChangesAsync();
                return Ok(
                    _mapper.Map<SteamProfileInventoryItem, ProfileInventoryItemDescriptionStackDTO>(result.Item, this)
                );
            }
            catch (SteamRequestException ex)
            {
                switch (ex.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        return BadRequest(ex.Message);
                    case HttpStatusCode.Forbidden:
                        return Unauthorized("Forbidden, please check that your Steam API Key is correct and the items you are combining all belong to you.");
                    default: throw;
                }
            }
        }

        /// <summary>
        /// Split inventory item in to multiple stacks
        /// </summary>
        /// <remarks>This API requires authentication and a Steam Web API key to use</remarks>
        /// <param name="profileId">Valid Steam ID64, Custom URL, or Profile URL</param>
        /// <param name="itemId">
        /// The item id to be split. The item must have a quantity of two or more. 
        /// The item must belong to your (currently authenticated) profile or the request will fail
        /// </param>
        /// <param name="quantity">
        /// The number of items to split out in to a new stack
        /// </param>
        /// <param name="stackNewItems">
        /// If true, new items will be stacked as efficently as possible. If false, items will be created unstack (i.e. as single items)
        /// </param>
        /// <param name="apiKey">
        /// Valid Steam Web API key with permission to modify the source and destination items.
        /// You can obtain your Steam API key from: https://steamcommunity.com/dev/apikey.
        /// Read https://scmm.app/privacy for more about how your Steam API key is handled.
        /// </param>
        /// <response code="200">If the inventory items were split successfully, response contains a list of the new item stacks and quantities.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="401">If the request is unauthenticated (login first), Steam API key is invalid, or the requested inventory item does not belong to the authenticated user.</response>
        /// <response code="404">If the inventory items cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(AuthorizationPolicies.User)]
        [HttpPut("{profileId}/inventory/item/{itemId}/split")]
        [ProducesResponseType(typeof(IEnumerable<ProfileInventoryItemDescriptionStackDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SplitInventoryItemStacks([FromHeader(Name = "Steam-Api-Key")] string apiKey, [FromRoute] string profileId, [FromRoute] ulong itemId, [FromBody] uint quantity, [FromQuery] bool stackNewItems = true)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                return BadRequest("Profile id is invalid");
            }
            if (itemId <= 0)
            {
                return BadRequest("Item id is invalid");
            }
            if (quantity <= 0)
            {
                return BadRequest($"Item quantity is invalid, must be greater than zero");
            }

            var item = await _db.SteamProfileInventoryItems.FirstOrDefaultAsync(x => x.SteamId == itemId.ToString());
            if (item == null)
            {
                _logger.LogError($"Item was not found");
                return NotFound($"Item  was not found");
            }
            if (item.Quantity <= quantity)
            {
                return BadRequest($"Item quantity is invalid, must be less than {item.Quantity}");
            }
            if (!User.Is(item.ProfileId))
            {
                _logger.LogError($"Item does not belong to you and you do not have permission to modify it");
                return Unauthorized($"Item does not belong to you and you do not have permission to modify it");
            }

            try
            {
                var result = await _commandProcessor.ProcessWithResultAsync(new SplitInventoryItemStackRequest()
                {
                    ProfileId = profileId,
                    ApiKey = apiKey,
                    ItemId = itemId,
                    Quantity = quantity,
                    StackNewItems = stackNewItems
                });

                await _db.SaveChangesAsync();
                return Ok(
                    _mapper.Map<SteamProfileInventoryItem, ProfileInventoryItemDescriptionStackDTO>(result.Items, this)
                );
            }
            catch (SteamRequestException ex)
            {
                switch (ex.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        return BadRequest(ex.Message);
                    case HttpStatusCode.Forbidden:
                        return Unauthorized("Forbidden, please check that your Steam API Key is correct and the item you are splitting belong to you.");
                    default: throw;
                }
            }
        }
    }
}
