using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Shared.Domain.DTOs;
using System;
using System.Threading.Tasks;

namespace SCMM.Web.Server.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("[controller]")]
    public class InventoryItemsController : ControllerBase
    {
        private readonly ILogger<MarketItemsController> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;

        public InventoryItemsController(ILogger<MarketItemsController> logger, IServiceScopeFactory scopeFactory, IMapper mapper)
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

                profile = await service.LoadAndRefreshProfileInventory(steamId);
                return _mapper.Map<SteamProfile, ProfileInventoryDetailsDTO>(profile);
            }
        }

        [HttpPut("item/{inventoryItemId}")]
        public async void SetItemBuyPrice([FromRoute] Guid inventoryItemId, [FromBody] int buyPrice)
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

                inventoryItem.BuyPrice = buyPrice;
                await db.SaveChangesAsync();
            }
        }
    }
}
