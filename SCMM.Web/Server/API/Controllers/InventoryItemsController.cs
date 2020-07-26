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
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("[controller]")]
    public class InventoryItemsController : ControllerBase
    {
        private readonly ILogger<InventoryItemsController> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;

        public InventoryItemsController(ILogger<InventoryItemsController> logger, IServiceScopeFactory scopeFactory, IMapper mapper)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _mapper = mapper;
        }

        [HttpGet("{steamId}")]
        public async Task<ProfileInventoryDetailsDTO> Get([FromRoute] string steamId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var service = scope.ServiceProvider.GetService<SteamService>();
                var profile = await service.AddOrUpdateSteamProfile(steamId);
                if (profile == null)
                {
                    _logger.LogError($"Profile with SteamID '{steamId}' was not found");
                    return null;
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

        [HttpPut("item/{inventoryItemId}")]
        public async void SetItemBuyPrice([FromRoute] Guid inventoryItemId, [FromBody] UpdateInventoryItemPriceCommand command)
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
