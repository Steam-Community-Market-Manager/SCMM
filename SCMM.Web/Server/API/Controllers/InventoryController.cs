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
using SCMM.Web.Shared.Domain.DTOs.InventoryItems;
using SCMM.Web.Shared;
using System;
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
                        .Include(x => x.InventoryItems).ThenInclude(x => x.MarketItem.Description)
                        .Include(x => x.InventoryItems).ThenInclude(x => x.MarketItem.Currency)
                        .Include(x => x.InventoryItems).ThenInclude(x => x.MarketItem.Activity)
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

                var mappedProfile = _mapper.Map<SteamProfile, ProfileInventoryDetailsDTO>(
                    await service.LoadAndRefreshProfileInventory(steamId),
                    Request
                );

                var inventoryValueHistory = await service.LoadInventoryValueHistory(steamId, Request.Currency());
                mappedProfile.ValueHistoryGraph = inventoryValueHistory.ToDictionary(
                    x => x.Key.ToString("dd MMM yyyy"),
                    x => x.Value
                );

                var inventoryProfitHistory = await service.LoadInventoryProfitHistory(steamId, Request.Currency());
                mappedProfile.ValueProfitGraph = inventoryProfitHistory.ToDictionary(
                    x => x.Key.ToString("dd MMM yyyy"),
                    x => x.Value
                );

                return mappedProfile;
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
                var defaultCurrency = SteamCurrencyService.GetDefaultCached();
                var profileInventory = await db.SteamProfiles
                    .Where(x => x.SteamId == steamId || x.ProfileId == steamId)
                    .Select(x => new
                    {
                        TotalItems = x.InventoryItems.Sum(x => x.Quantity),
                        TotalInvested = x.InventoryItems.Sum(x => x.BuyPrice / x.Currency.ExchangeRateMultiplier),
                        TotalMarketValue = x.InventoryItems.Select(x => x.MarketItem).Sum(x => x.Last1hrValue),
                        TotalResellValue = x.InventoryItems.Select(x => x.MarketItem).Sum(x => x.ResellPrice)
                    })
                    .FirstOrDefaultAsync();

                if (profileInventory == null)
                {
                    throw new Exception($"Profile with SteamID '{steamId}' was not found");
                }

                return new ProfileInventoryTotalsDTO()
                {
                    TotalItems = profileInventory.TotalItems,
                    TotalInvested = Request.Currency().CalculateExchange(profileInventory.TotalInvested ?? 0),
                    TotalMarketValue = Request.Currency().CalculateExchange(profileInventory.TotalMarketValue, defaultCurrency),
                    TotalResellValue = Request.Currency().CalculateExchange(profileInventory.TotalResellValue, defaultCurrency),
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
