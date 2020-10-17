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
using SCMM.Web.Server.Services;
using SCMM.Web.Shared;
using SCMM.Web.Shared.Domain.DTOs.InventoryItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly ILogger<InventoryController> _logger;
        private readonly SteamDbContext _db;
        private readonly SteamService _steam;
        private readonly IMapper _mapper;

        public InventoryController(ILogger<InventoryController> logger, SteamDbContext db, SteamService steam, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _steam = steam;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProfileInventoryDetailsDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyInventoryProfile([FromQuery] bool sync = false)
        {
            return await GetInventoryProfile(User.SteamId(), sync);
        }

        [AllowAnonymous]
        [HttpGet("{steamId}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProfileInventoryDetailsDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInventoryProfile([FromRoute] string steamId, [FromQuery] bool sync = false)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return NotFound();
            }

            // Load the profile
            var inventory = _db.SteamProfiles
                .Where(x => x.SteamId == steamId || x.ProfileId == steamId)
                .Select(x => new
                {
                    Profile = x,
                    TotalItems = x.InventoryItems.Count,
                    LastUpdatedOn = x.LastUpdatedInventoryOn
                })
                .FirstOrDefault();

            // If the profile inventory hasn't been loaded before or it is older than the cache period, fetch it now
            var profile = inventory?.Profile;
            if (profile == null || inventory?.TotalItems == 0 || inventory?.LastUpdatedOn < DateTime.Now.Subtract(TimeSpan.FromHours(6)) || sync)
            {
                // Load the profile and force an inventory sync
                profile = await _steam.AddOrUpdateSteamProfile(steamId, fetchLatest: true);
                profile = await _steam.FetchProfileInventory(steamId);
            }

            if (profile == null)
            {
                NotFound($"Profile with SteamID '{steamId}' was not found");
            }

            if (User.Is(profile))
            {
                profile.LastViewedInventoryOn = DateTimeOffset.Now;
                _db.SaveChanges();
            }

            return Ok(
                _mapper.Map<SteamProfile, ProfileInventoryDetailsDTO>(
                    profile, this
                )
            );
        }

        [AllowAnonymous]
        [HttpGet("me/total")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProfileInventoryTotalsDTO), StatusCodes.Status200OK)]
        public IActionResult GetMyInventoryTotal()
        {
            return GetInventoryTotal(User.SteamId());
        }

        [AllowAnonymous]
        [HttpGet("{steamId}/total")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProfileInventoryTotalsDTO), StatusCodes.Status200OK)]
        public IActionResult GetInventoryTotal([FromRoute] string steamId)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return NotFound();
            }

            var currency = this.Currency();
            var profileInventoryItems = _db.SteamInventoryItems
                .Where(x => x.Owner.SteamId == steamId || x.Owner.ProfileId == steamId)
                .Where(x => x.MarketItemId != null)
                .Select(x => new
                {
                    Quantity = x.Quantity,
                    BuyPrice = x.BuyPrice,
                    ExchangeRateMultiplier = x.Currency.ExchangeRateMultiplier,
                    MarketItemLast1hrValue = x.MarketItem.Last1hrValue,
                    MarketItemLast24hrValue = x.MarketItem.Last24hrValue,
                    MarketItemResellPrice = x.MarketItem.ResellPrice,
                    MarketItemResellTax = x.MarketItem.ResellTax,
                    MarketItemExchangeRateMultiplier = x.MarketItem.Currency.ExchangeRateMultiplier
                })
                .ToList();

            var profileInventory = new
            {
                TotalItems = profileInventoryItems
                    .Sum(x => x.Quantity),
                TotalInvested = profileInventoryItems
                    .Where(x => x.BuyPrice != null && x.BuyPrice != 0 && x.ExchangeRateMultiplier != 0)
                    .Sum(x => (x.BuyPrice / x.ExchangeRateMultiplier) * x.Quantity),
                TotalMarketValueLast1hr = profileInventoryItems
                    .Where(x => x.MarketItemLast1hrValue != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemLast1hrValue / x.MarketItemExchangeRateMultiplier) * x.Quantity),
                TotalMarketValueLast24hr = profileInventoryItems
                    .Where(x => x.MarketItemLast24hrValue != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemLast24hrValue / x.MarketItemExchangeRateMultiplier) * x.Quantity),
                TotalResellValue = profileInventoryItems
                    .Where(x => x.MarketItemResellPrice != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemResellPrice / x.MarketItemExchangeRateMultiplier) * x.Quantity),
                TotalResellTax = profileInventoryItems
                    .Where(x => x.MarketItemResellTax != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemResellTax / x.MarketItemExchangeRateMultiplier) * x.Quantity)
            };

            return Ok(
                new ProfileInventoryTotalsDTO()
                {
                    TotalItems = profileInventory.TotalItems,
                    TotalInvested = currency.CalculateExchange(profileInventory.TotalInvested ?? 0),
                    TotalMarketValue = currency.CalculateExchange(profileInventory.TotalMarketValueLast1hr),
                    TotalMarket24hrMovement = currency.CalculateExchange(profileInventory.TotalMarketValueLast1hr - profileInventory.TotalMarketValueLast24hr),
                    TotalResellValue = currency.CalculateExchange(profileInventory.TotalResellValue),
                    TotalResellTax = currency.CalculateExchange(profileInventory.TotalResellTax),
                    TotalResellProfit = (
                        currency.CalculateExchange(profileInventory.TotalResellValue - profileInventory.TotalResellTax) - currency.CalculateExchange(profileInventory.TotalInvested ?? 0)
                    )
                }
            );
        }

        [AllowAnonymous]
        [HttpGet("me/summary")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(IList<ProfileInventoryItemSummaryDTO>), StatusCodes.Status200OK)]
        public IActionResult GetMyInventorySummary()
        {
            return GetInventorySummary(User.SteamId());
        }

        [AllowAnonymous]
        [HttpGet("{steamId}/summary")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(IList<ProfileInventoryItemSummaryDTO>), StatusCodes.Status200OK)]
        public IActionResult GetInventorySummary([FromRoute] string steamId)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return NotFound();
            }

            var profileInventoryItems = _db.SteamInventoryItems
                .Where(x => x.Owner.SteamId == steamId || x.Owner.ProfileId == steamId)
                .Where(x => x.Description != null && x.Description.SteamId != null)
                .Include(x => x.Description)
                .Include(x => x.Description.App)
                .Include(x => x.Description.StoreItem)
                .Include(x => x.Description.MarketItem)
                .Include(x => x.Description.MarketItem.Currency)
                .ToList();

            var profileInventoryItemsSummaries = new List<ProfileInventoryItemSummaryDTO>();
            foreach (var item in profileInventoryItems)
            {
                if (!profileInventoryItemsSummaries.Any(x => x.SteamId == item.Description.SteamId))
                {
                    var itemSummary = _mapper.Map<SteamAssetDescription, ProfileInventoryItemSummaryDTO>(
                        item.Description, this
                    );

                    // Calculate the item's value
                    if (item.Description.MarketItem != null && item.Description.MarketItem.Currency != null)
                    {
                        itemSummary.Value = this.Currency().CalculateExchange(
                            item.Description.MarketItem.Last1hrValue, item.Description.MarketItem.Currency
                        );
                    }
                    else if (item.Description.StoreItem != null)
                    {
                        itemSummary.Value = item.Description.StoreItem.StorePrices.FirstOrDefault(x => x.Key == this.Currency().Name).Value;
                    }

                    // Calculate the item's quantity
                    itemSummary.Quantity = profileInventoryItems
                        .Where(x => x.Description.SteamId == item.Description.SteamId)
                        .Sum(x => x.Quantity);

                    profileInventoryItemsSummaries.Add(itemSummary);
                }
            }

            return Ok(
                profileInventoryItemsSummaries.OrderByDescending(x => x.Value)
            );
        }

        [AllowAnonymous]
        [HttpGet("me/returnOnInvestment")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(IList<InventoryItemListDTO>), StatusCodes.Status200OK)]
        public IActionResult GetMyInventoryInvestment()
        {
            return GetInventoryInvestment(User.SteamId());
        }

        [AllowAnonymous]
        [HttpGet("{steamId}/returnOnInvestment")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(IList<InventoryItemListDTO>), StatusCodes.Status200OK)]
        public IActionResult GetInventoryInvestment([FromRoute] string steamId)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return NotFound();
            }

            var profileInventoryItems = _db.SteamInventoryItems
                .Where(x => x.Owner.SteamId == steamId || x.Owner.ProfileId == steamId)
                .Select(x => new
                {
                    Item = x,
                    ItemDescription = x.Description,
                    ItemCurrency = x.Currency,
                    MarketItem = x.MarketItem,
                    MarketItemApp = x.MarketItem.App,
                    MarketItemDescription = x.MarketItem.Description,
                    MarketItemCurrency = x.MarketItem.Currency,
                    HasBuyPrice = (x.BuyPrice > 0),
                    ReturnOnInvestment = (x.BuyPrice > 0 && (x.MarketItem.ResellPrice - x.MarketItem.ResellTax) > 0
                        ? ((decimal)(x.MarketItem.ResellPrice - x.MarketItem.ResellTax) / x.BuyPrice)
                        : 0
                    )
                })
                .OrderByDescending(x => x.ReturnOnInvestment)
                .ToList();

            var profileInventoryItemsDetails = new List<InventoryItemListDTO>();
            foreach (var profileInventoryItem in profileInventoryItems)
            {
                profileInventoryItemsDetails.Add(
                    _mapper.Map<SteamInventoryItem, InventoryItemListDTO>(
                        profileInventoryItem.Item, this
                    )
                );
            }

            return Ok(profileInventoryItemsDetails);
        }

        [AllowAnonymous]
        [HttpGet("me/activity")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(IList<ProfileInventoryActivityDTO>), StatusCodes.Status200OK)]
        public IActionResult GetMyInventoryActivity()
        {
            return GetInventoryActivity(User.SteamId());
        }

        [AllowAnonymous]
        [HttpGet("{steamId}/activity")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(IList<ProfileInventoryActivityDTO>), StatusCodes.Status200OK)]
        public IActionResult GetInventoryActivity([FromRoute] string steamId)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return NotFound();
            }

            var recentActivityCutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(24));
            var profileInventoryActivities = _db.SteamInventoryItems
                .Where(x => x.Owner.SteamId == steamId || x.Owner.ProfileId == steamId)
                .Where(x => x.MarketItem != null)
                .SelectMany(x =>
                    x.MarketItem.Activity.Where(x => x.Timestamp >= recentActivityCutoff)
                )
                .OrderByDescending(x => x.Timestamp)
                .Include(x => x.Item)
                .Include(x => x.Item.Description)
                .Include(x => x.Item.Currency)
                .Take(100)
                .ToList();

            var profileInventoryActivitiesDetails = new List<ProfileInventoryActivityDTO>();
            foreach (var profileInventoryActivity in profileInventoryActivities)
            {
                profileInventoryActivitiesDetails.Add(
                    _mapper.Map<SteamMarketItemActivity, ProfileInventoryActivityDTO>(
                        profileInventoryActivity, this
                    )
                );
            }

            return Ok(profileInventoryActivitiesDetails);
        }

        [AllowAnonymous]
        [HttpGet("me/performance")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProfileInventoryPerformanceDTO), StatusCodes.Status200OK)]
        public IActionResult GetMyInventoryPerformance()
        {
            return GetInventoryPerformance(User.SteamId());
        }

        [AllowAnonymous]
        [HttpGet("{steamId}/performance")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProfileInventoryPerformanceDTO), StatusCodes.Status200OK)]
        public IActionResult GetInventoryPerformance([FromRoute] string steamId)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return NotFound();
            }

            var currency = this.Currency();
            var profileInventoryItems = _db.SteamInventoryItems
                .Where(x => x.Owner.SteamId == steamId || x.Owner.ProfileId == steamId)
                .Where(x => x.MarketItemId != null)
                .Select(x => new
                {
                    Quantity = x.Quantity,
                    BuyPrice = x.BuyPrice,
                    ExchangeRateMultiplier = x.Currency.ExchangeRateMultiplier,
                    MarketItemLast1hrValue = x.MarketItem.Last1hrValue,
                    MarketItemLast24hrValue = x.MarketItem.Last24hrValue,
                    MarketItemLast48hrValue = x.MarketItem.Last48hrValue,
                    MarketItemLast72hrValue = x.MarketItem.Last72hrValue,
                    MarketItemLast96hrValue = x.MarketItem.Last96hrValue,
                    MarketItemLast120hrValue = x.MarketItem.Last120hrValue,
                    MarketItemLast144hrValue = x.MarketItem.Last144hrValue,
                    MarketItemLast168hrValue = x.MarketItem.Last168hrValue,
                    MarketItemExchangeRateMultiplier = x.MarketItem.Currency.ExchangeRateMultiplier
                })
                .ToList();

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
            profitHistory[today.Subtract(TimeSpan.FromDays(7))] = last168hrValue - SteamEconomyHelper.GetSteamFeeAsInt(last168hrValue) - totalInvested;
            profitHistory[today.Subtract(TimeSpan.FromDays(6))] = last144hrValue - SteamEconomyHelper.GetSteamFeeAsInt(last144hrValue) - totalInvested;
            profitHistory[today.Subtract(TimeSpan.FromDays(5))] = last120hrValue - SteamEconomyHelper.GetSteamFeeAsInt(last120hrValue) - totalInvested;
            profitHistory[today.Subtract(TimeSpan.FromDays(4))] = last96hrValue - SteamEconomyHelper.GetSteamFeeAsInt(last96hrValue) - totalInvested;
            profitHistory[today.Subtract(TimeSpan.FromDays(3))] = last72hrValue - SteamEconomyHelper.GetSteamFeeAsInt(last72hrValue) - totalInvested;
            profitHistory[today.Subtract(TimeSpan.FromDays(2))] = last48hrValue - SteamEconomyHelper.GetSteamFeeAsInt(last48hrValue) - totalInvested;
            profitHistory[today.Subtract(TimeSpan.FromDays(1))] = last24hrValue - SteamEconomyHelper.GetSteamFeeAsInt(last24hrValue) - totalInvested;
            profitHistory[today.Subtract(TimeSpan.FromDays(0))] = last1hrValue - SteamEconomyHelper.GetSteamFeeAsInt(last1hrValue) - totalInvested;

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

        [Authorize]
        [HttpPut("item/{inventoryItemId}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult SetInventoryItemBuyPrice([FromRoute] Guid inventoryItemId, [FromBody] UpdateInventoryItemPriceCommand command)
        {
            if (command == null || command.CurrencyId == Guid.Empty)
            {
                return BadRequest();
            }
            if (inventoryItemId == Guid.Empty)
            {
                return NotFound();
            }

            var inventoryItem = _db.SteamInventoryItems.FirstOrDefault(x => x.Id == inventoryItemId);
            if (inventoryItem == null)
            {
                _logger.LogError($"Inventory item with id '{inventoryItemId}' was not found");
                return NotFound();
            }
            if (!User.Is(inventoryItem.OwnerId))
            {
                _logger.LogError($"Inventory item with id '{inventoryItemId}' does not belong to you");
                return NotFound();
            }

            inventoryItem.CurrencyId = command.CurrencyId;
            inventoryItem.BuyPrice = SteamEconomyHelper.GetPriceValueAsInt(command.Price);
            _db.SaveChanges();
            return Ok();
        }
    }
}
