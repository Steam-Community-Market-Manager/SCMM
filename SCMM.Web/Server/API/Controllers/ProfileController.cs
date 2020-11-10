using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Shared;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Shared.Data.Models.Steam;
using SCMM.Web.Shared.Domain.DTOs.InventoryItems;
using SCMM.Web.Shared.Domain.DTOs.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly ILogger<ProfileController> _logger;
        private readonly ScmmDbContext _db;
        private readonly IMapper _mapper;

        public ProfileController(ILogger<ProfileController> logger, ScmmDbContext db, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet("donators")]
        public IEnumerable<ProfileDTO> GetDonators()
        {
            return _db.SteamProfiles
                .Where(x => x.DonatorLevel > 0)
                .OrderByDescending(x => x.DonatorLevel)
                .Select(x => _mapper.Map<ProfileDTO>(x))
                .ToList();
        }

        [AllowAnonymous]
        [HttpGet("me")]
        public ProfileDetailedDTO GetMyProfile()
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
                var profile = _db.SteamProfiles
                    .Include(x => x.Language)
                    .Include(x => x.Currency)
                    .FirstOrDefault(x => x.Id == profileId);

                // Map the DB profile over top of the default profile
                // NOTE: This is done so that the language/currency pass-through if they haven't been set yet
                var authenticatedProfile = _mapper.Map<ProfileDetailedDTO>(profile);
                authenticatedProfile.Language = (authenticatedProfile.Language ?? defaultProfile.Language);
                authenticatedProfile.Currency = (authenticatedProfile.Currency ?? defaultProfile.Currency);
                return authenticatedProfile;
            }

            // Else, use a transient guest profile
            else
            {
                return defaultProfile;
            }
        }

        [Authorize]
        [HttpPut("me")]
        public void SetMyProfile([FromBody] UpdateProfileCommand command)
        {
            var profileId = User.Id();
            var profile = _db.SteamProfiles
                .Include(x => x.Language)
                .Include(x => x.Currency)
                .FirstOrDefault(x => x.Id == profileId);

            if (profile == null)
            {
                throw new Exception($"Profile with Steam ID '{User.SteamId()}' was not found");
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

            _db.SaveChanges();
        }

        #region Inventory Items

        [Authorize]
        [HttpPut("inventory/item/{itemId}/buyPrice")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult SetInventoryItemBuyPrice([FromRoute] Guid itemId, [FromBody] UpdateInventoryItemPriceCommand command)
        {
            if (command == null || command.CurrencyId == Guid.Empty)
            {
                return BadRequest();
            }
            if (itemId == Guid.Empty)
            {
                return NotFound();
            }

            var inventoryItem = _db.SteamProfileInventoryItems.FirstOrDefault(x => x.Id == itemId);
            if (inventoryItem == null)
            {
                _logger.LogError($"Inventory item with id '{itemId}' was not found");
                return NotFound();
            }
            if (!User.Is(inventoryItem.ProfileId))
            {
                _logger.LogError($"Inventory item with id '{itemId}' does not belong to you");
                return NotFound();
            }

            inventoryItem.CurrencyId = command.CurrencyId;
            inventoryItem.BuyPrice = SteamEconomyHelper.GetPriceValueAsInt(command.Price);
            _db.SaveChanges();
            return Ok();
        }

        [Authorize]
        [HttpPut("inventory/item/{itemOrAssetId}/{flag}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult SetInventoryItemFlag([FromRoute] string itemOrAssetId, [FromRoute] string flag, [FromBody] bool value)
        {
            SteamProfileInventoryItemFlags flagValue;
            if (!Enum.TryParse(flag, true, out flagValue))
            {
                return BadRequest();
            }
            if (String.IsNullOrEmpty(itemOrAssetId))
            {
                return NotFound();
            }

            var profileId = User.Id();
            var inventoryItems = _db.SteamProfileInventoryItems
                .Where(x => x.ProfileId == profileId)
                .Where(x => x.SteamId == itemOrAssetId || x.Description.SteamId == itemOrAssetId)
                .ToList();
            if (!(inventoryItems?.Any() == true))
            {
                _logger.LogError($"No inventory items found that match id '{itemOrAssetId}' for your profile");
                return NotFound();
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

            _db.SaveChanges();
            return Ok();
        }

        #endregion

        #region Market Items

        [Authorize]
        [HttpPut("market/item/{itemOrAssetId}/{flag}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult SetMarketItemFlag([FromRoute] string itemOrAssetId, [FromRoute] string flag, [FromBody] bool value)
        {
            SteamProfileMarketItemFlags flagValue;
            if (!Enum.TryParse(flag, true, out flagValue))
            {
                return BadRequest();
            }
            if (String.IsNullOrEmpty(itemOrAssetId))
            {
                return NotFound();
            }

            var profileId = User.Id();
            var marketItems = _db.SteamProfileMarketItems
                .Where(x => x.ProfileId == profileId)
                .Where(x => x.SteamId == itemOrAssetId || x.Description.SteamId == itemOrAssetId)
                .ToList();

            if (!(marketItems?.Any() == true))
            {
                var assetDescription = _db.SteamAssetDescriptions
                    .Where(x => x.SteamId == itemOrAssetId)
                    .FirstOrDefault();
                if (assetDescription == null)
                {
                    _logger.LogError($"No asset description that match id '{itemOrAssetId}' was found");
                    return NotFound();
                }

                var marketItem = new SteamProfileMarketItem()
                {
                    SteamId = null,
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

            _db.SaveChanges();
            return Ok();
        }

        #endregion
    }
}
