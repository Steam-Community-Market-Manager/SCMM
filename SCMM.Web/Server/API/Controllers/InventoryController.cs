using AutoMapper;
using CommandQuery;
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
using SCMM.Web.Server.Services.Commands;
using SCMM.Web.Server.Services.Queries;
using SCMM.Web.Shared;
using SCMM.Web.Shared.Data.Models.Steam;
using SCMM.Web.Shared.Data.Models.UI;
using SCMM.Web.Shared.Data.Models.UI.ProfileInventory;
using SCMM.Web.Shared.Domain.DTOs.InventoryItems;
using Skclusive.Core.Component;
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
        private readonly ScmmDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        private readonly SteamService _steam;

        public InventoryController(ILogger<InventoryController> logger, ScmmDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper, SteamService steam)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
            _steam = steam;
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
            var fetchAndCreateProfile = await _commandProcessor.ProcessWithResultAsync(new FetchAndCreateSteamProfileRequest()
            {
                Id = steamId
            });

            var profile = fetchAndCreateProfile?.Profile;
            if (profile == null)
            {
                NotFound($"Profile with SteamID '{steamId}' was not found");
            }

            // Reload the profile's inventory
            await _commandProcessor.ProcessAsync(new FetchSteamProfileInventoryRequest()
            {
                Id = profile.Id,
                Force = sync
            });

            // Update the profile's inventory last viewed time
            if (User.Is(profile))
            {
                profile.LastViewedInventoryOn = DateTimeOffset.Now;
            }

            _db.SaveChanges();

            return Ok(
                _mapper.Map<SteamProfile, ProfileInventoryDetailsDTO>(
                    profile, this
                )
            );
        }

        [AllowAnonymous]
        [HttpGet("{steamId}/total")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProfileInventoryTotalsDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInventoryTotal([FromRoute] string steamId)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return NotFound();
            }

            var inventoryTotal = await _queryProcessor.ProcessAsync(new GetSteamProfileInventoryTotalsRequest()
            {
                SteamId = steamId,
                CurrencyId = this.Currency().SteamId
            });

            if (inventoryTotal == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(
                    _mapper.Map<GetSteamProfileInventoryTotalsResponse, ProfileInventoryTotalsDTO>(inventoryTotal, this)    
                );
            }
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

            var profileInventoryItems = _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.Profile.SteamId == steamId || x.Profile.ProfileId == steamId)
                .Where(x => x.Description != null && x.Description.SteamId != null)
                .Include(x => x.Description)
                .Include(x => x.Description.App)
                .Include(x => x.Description.StoreItem)
                .Include(x => x.Description.StoreItem.Prices)
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
                    if (item.Description.MarketItem?.Last1hrValue > 0 && item.Description.MarketItem.Currency != null)
                    {
                        itemSummary.Value = this.Currency().CalculateExchange(
                            item.Description.MarketItem.Last1hrValue, item.Description.MarketItem.Currency
                        );
                    }
                    else if (item.Description.StoreItem?.Prices != null)
                    {
                        itemSummary.Value = item.Description.StoreItem.Prices.FirstOrDefault(x => x.Key == this.Currency().Name).Value;
                    }

                    // Calculate the item's quantity
                    itemSummary.Quantity = profileInventoryItems
                        .Where(x => x.Description.SteamId == item.Description.SteamId)
                        .Sum(x => x.Quantity);

                    // Calculate the item's flags
                    itemSummary.Flags = profileInventoryItems
                        .Where(x => x.Description.SteamId == item.Description.SteamId)
                        .Select(x => x.Flags)
                        .Aggregate((x, y) => x | y);

                    profileInventoryItemsSummaries.Add(itemSummary);
                }
            }

            return Ok(
                profileInventoryItemsSummaries.OrderByDescending(x => x.Value)
            );
        }

        [AllowAnonymous]
        [HttpGet("{steamId}/returnOnInvestment")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(PaginatedResult<InventoryInvestmentItemDTO>), StatusCodes.Status200OK)]
        public IActionResult GetInventoryInvestment([FromRoute] string steamId, [FromQuery] string filter = null, [FromQuery] int start = 0, [FromQuery] int count = 10, [FromQuery] string sortBy = null, [FromQuery] Sort sortDirection = Sort.Ascending)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return BadRequest();
            }

            filter = Uri.UnescapeDataString(filter?.Trim() ?? String.Empty);
            var query = _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.Profile.SteamId == steamId || x.Profile.ProfileId == steamId)
                .Where(x => String.IsNullOrEmpty(filter) || x.Description.Name.ToLower().Contains(filter.ToLower()))
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description.MarketItem)
                .Include(x => x.Description.MarketItem.Currency)
                .Include(x => x.Description.StoreItem)
                .Include(x => x.Description.StoreItem.Currency)
                .OrderBy(sortBy, sortDirection);

            var results = query.Paginate(start, count,
                x => _mapper.Map<SteamProfileInventoryItem, InventoryInvestmentItemDTO>(x, this)
            );

            return Ok(results);
        }
        
        [AllowAnonymous]
        [HttpGet("{steamId}/wishlist")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(IList<ProfileInventoryItemWishDTO>), StatusCodes.Status200OK)]
        public IActionResult GetInventoryWishlist([FromRoute] string steamId)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                return NotFound();
            }

            var profileMarketItems = _db.SteamProfileMarketItems
                .AsNoTracking()
                .Where(x => x.Profile.SteamId == steamId || x.Profile.ProfileId == steamId)
                .Where(x => x.Flags.HasFlag(SteamProfileMarketItemFlags.WantToBuy))
                .Include(x => x.App)
                .Include(x => x.Description.MarketItem)
                .Include(x => x.Description.MarketItem.Currency)
                .OrderBy(x => x.Description.Name)
                .ToList();

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
            var profileInventoryItems = _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.Profile.SteamId == steamId || x.Profile.ProfileId == steamId)
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
    }
}
