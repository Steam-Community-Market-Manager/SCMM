using AngleSharp.Common;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Shared;
using SCMM.Web.Server.API.Controllers.Extensions;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Shared;
using SCMM.Web.Shared.Domain.DTOs.InventoryItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly ILogger<InventoryController> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;

        public InventoryController(ILogger<InventoryController> logger, IServiceScopeFactory scopeFactory, IMapper mapper)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _mapper = mapper;
        }

        [HttpGet("me")]
        public async Task<ProfileInventoryDetailsDTO> GetMyInventory([FromQuery] bool sync = false)
        {
            return await GetInventory(Request.ProfileId(), sync);
        }

        [HttpGet("{steamId}")]
        public async Task<ProfileInventoryDetailsDTO> GetInventory([FromRoute] string steamId, [FromQuery] bool sync = false)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                throw new ArgumentNullException(nameof(steamId));
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var service = scope.ServiceProvider.GetService<SteamService>();
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var currency = Request.Currency();
                var profile = (SteamProfile)null;

                if (sync)
                {
                    // Force inventory sync
                    profile = await service.LoadAndRefreshProfileInventory(steamId);
                }
                else
                {
                    // Use last cached inventory
                    profile = await db.SteamProfiles
                        .Include(x => x.InventoryItems).ThenInclude(x => x.App)
                        .Include(x => x.InventoryItems).ThenInclude(x => x.Description)
                        .Include(x => x.InventoryItems).ThenInclude(x => x.Currency)
                        .Include(x => x.InventoryItems).ThenInclude(x => x.MarketItem)
                        .Include(x => x.InventoryItems).ThenInclude(x => x.MarketItem.App)
                        .Include(x => x.InventoryItems).ThenInclude(x => x.MarketItem.Description)
                        .Include(x => x.InventoryItems).ThenInclude(x => x.MarketItem.Currency)
                        .FirstOrDefaultAsync(x => x.SteamId == steamId || x.ProfileId == steamId);

                    // Profile doesn't exist yet? force sync
                    if (profile == null)
                    {
                        profile = await service.LoadAndRefreshProfileInventory(steamId);
                    }
                }

                if (profile == null)
                {
                    throw new Exception($"Profile with SteamID '{steamId}' was not found");
                }

                return _mapper.Map<SteamProfile, ProfileInventoryDetailsDTO>(
                    profile, Request
                );
            }
        }

        [HttpGet("me/performance")]
        public async Task<ProfileInventoryPerformanceDTO> GetMyInventoryPerformance([FromQuery] bool sync = false)
        {
            return await GetInventoryPerformance(Request.ProfileId(), sync);
        }

        [HttpGet("{steamId}/performance")]
        public async Task<ProfileInventoryPerformanceDTO> GetInventoryPerformance([FromRoute] string steamId, [FromQuery] bool sync = false)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                throw new ArgumentNullException(nameof(steamId));
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var service = scope.ServiceProvider.GetService<SteamService>();
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var currency = Request.Currency();

                var profileInventory = await db.SteamProfiles
                    .Where(x => x.SteamId == steamId || x.ProfileId == steamId)
                    .Select(x => new
                    {
                        Invested = x.InventoryItems.Sum(x => x.BuyPrice / x.Currency.ExchangeRateMultiplier),
                        Last1hrValue = x.InventoryItems.Select(x => x.MarketItem).Sum(x => x.Last1hrValue / x.Currency.ExchangeRateMultiplier),
                        Last24hrValue = x.InventoryItems.Select(x => x.MarketItem).Sum(x => x.Last24hrValue / x.Currency.ExchangeRateMultiplier),
                        Last48hrValue = x.InventoryItems.Select(x => x.MarketItem).Sum(x => x.Last48hrValue / x.Currency.ExchangeRateMultiplier),
                        Last72hrValue = x.InventoryItems.Select(x => x.MarketItem).Sum(x => x.Last72hrValue / x.Currency.ExchangeRateMultiplier),
                        Last96hrValue = x.InventoryItems.Select(x => x.MarketItem).Sum(x => x.Last96hrValue / x.Currency.ExchangeRateMultiplier),
                        Last120hrValue = x.InventoryItems.Select(x => x.MarketItem).Sum(x => x.Last120hrValue / x.Currency.ExchangeRateMultiplier),
                        Last144hrValue = x.InventoryItems.Select(x => x.MarketItem).Sum(x => x.Last144hrValue / x.Currency.ExchangeRateMultiplier),
                        Last168hrValue = x.InventoryItems.Select(x => x.MarketItem).Sum(x => x.Last168hrValue / x.Currency.ExchangeRateMultiplier)
                    })
                    .FirstOrDefaultAsync();

                if (profileInventory == null)
                {
                    throw new Exception($"Profile with SteamID '{steamId}' was not found");
                }

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

                return new ProfileInventoryPerformanceDTO()
                {
                    ValueHistoryGraph = valueHistory.ToDictionary(
                        x => x.Key.ToString("dd MMM yyyy"),
                        x => x.Value
                    ),
                    ProfitHistoryGraph = profitHistory.ToDictionary(
                        x => x.Key.ToString("dd MMM yyyy"),
                        x => x.Value
                    )
                };
            }
        }

        [HttpGet("me/total")]
        public async Task<ProfileInventoryTotalsDTO> GetMyInventoryTotal()
        {
            return await GetInventoryTotal(Request.ProfileId());
        }

        [HttpGet("{steamId}/total")]
        public async Task<ProfileInventoryTotalsDTO> GetInventoryTotal([FromRoute] string steamId)
        {
            if (String.IsNullOrEmpty(steamId))
            {
                throw new ArgumentNullException(nameof(steamId));
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var currency = Request.Currency();
                var profileInventory = await db.SteamProfiles
                    .Where(x => x.SteamId == steamId || x.ProfileId == steamId)
                    .Select(x => new
                    {
                        TotalItems = x.InventoryItems.Sum(x => x.Quantity),
                        TotalInvested = x.InventoryItems.Sum(x => x.BuyPrice / x.Currency.ExchangeRateMultiplier),
                        TotalMarketValueLast1hr = x.InventoryItems.Select(x => x.MarketItem).Sum(x => x.Last1hrValue / x.Currency.ExchangeRateMultiplier),
                        TotalMarketValueLast24hr = x.InventoryItems.Select(x => x.MarketItem).Sum(x => x.Last24hrValue / x.Currency.ExchangeRateMultiplier),
                        TotalResellValue = x.InventoryItems.Select(x => x.MarketItem).Sum(x => x.ResellPrice / x.Currency.ExchangeRateMultiplier),
                        TotalResellValueAfterTax = x.InventoryItems.Select(x => x.MarketItem).Sum(x => (x.ResellPrice - x.ResellTax) / x.Currency.ExchangeRateMultiplier)
                    })
                    .FirstOrDefaultAsync();

                if (profileInventory == null)
                {
                    throw new Exception($"Profile with SteamID '{steamId}' was not found");
                }

                return new ProfileInventoryTotalsDTO()
                {
                    TotalItems = profileInventory.TotalItems,
                    TotalInvested = currency.CalculateExchange(profileInventory.TotalInvested ?? 0),
                    TotalMarketValue = currency.CalculateExchange(profileInventory.TotalMarketValueLast1hr),
                    TotalMarket24hrMovement = currency.CalculateExchange(profileInventory.TotalMarketValueLast1hr - profileInventory.TotalMarketValueLast24hr),
                    TotalResellValue = currency.CalculateExchange(profileInventory.TotalResellValue),
                    TotalResellProfit = (
                        currency.CalculateExchange(profileInventory.TotalResellValueAfterTax) - currency.CalculateExchange(profileInventory.TotalInvested ?? 0)
                    )
                };
            }
        }

        [HttpPut("item/{inventoryItemId}")]
        public async void SetInventoryItemBuyPrice([FromRoute] Guid inventoryItemId, [FromBody] UpdateInventoryItemPriceCommand command)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var inventoryItem = await db.SteamInventoryItems.SingleOrDefaultAsync(x => x.Id == inventoryItemId);
                if (inventoryItem == null)
                {
                    _logger.LogError($"Inventory item with id '{inventoryItemId}' was not found");
                    return;
                }

                inventoryItem.CurrencyId = command.CurrencyId;
                inventoryItem.BuyPrice = SteamEconomyHelper.GetPriceValueAsInt(command.Price);
                await db.SaveChangesAsync();
            }
        }
    }
}
