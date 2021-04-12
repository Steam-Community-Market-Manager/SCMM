﻿using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Web.Data.Models.Domain.DTOs.StoreItems;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services;
using SCMM.Web.Server.Services.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/store")]
    public class StoreController : ControllerBase
    {
        private readonly ILogger<StoreController> _logger;
        private readonly ScmmDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        private readonly SteamService _steam;

        public StoreController(ILogger<StoreController> logger, ScmmDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper, SteamService steam)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
            _steam = steam;
        }

        /// <summary>
        /// List all known item store identifiers
        /// </summary>
        /// <returns>List of stores</returns>
        /// <response code="200">List of known item stores. Use <see cref="GetStore(Guid)"/> <code>/store/{storeId}</code> to get the detailed contents of an item store.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ItemStoreListDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStores()
        {
            var appId = this.App();
            var itemStores = await _db.SteamItemStores
                .AsNoTracking()
                .Where(x => x.App.SteamId == appId)
                .OrderBy(x => x.Start)
                .ToListAsync();

            return Ok(
                itemStores.Select(x => _mapper.Map<SteamItemStore, ItemStoreListDTO>(x, this)).ToList()
            );
        }

        /// <summary>
        /// Get the most recent item store details
        /// </summary>
        /// <remarks>
        /// There may be multiple active item stores, only the most recent is returned.
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <returns>The most recent item store</returns>
        /// <response code="200">The most recent item store.</response>
        /// <response code="404">If there are no currently active item stores.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("current")]
        [ProducesResponseType(typeof(ItemStoreDetailedDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCurrentStore()
        {
            var appId = this.App();
            var latestItemStoreId = await _db.SteamItemStores
                .AsNoTracking()
                .Where(x => x.App.SteamId == appId)
                .Where(x => x.End == null)
                .OrderByDescending(x => x.Start)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (latestItemStoreId == Guid.Empty)
            {
                return NotFound();
            }

            return await GetStore(latestItemStoreId);
        }

        /// <summary>
        /// Get an item store details
        /// </summary>
        /// <param name="storeId">Id of a known item store</param>
        /// <returns>The item store details</returns>
        /// <remarks>The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').</remarks>
        /// <response code="200">The store details.</response>
        /// <response code="404">If the store cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{storeId}")]
        [ProducesResponseType(typeof(ItemStoreDetailedDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStore([FromRoute] Guid storeId)
        {
            var itemStore = await _db.SteamItemStores
                .AsNoTracking()
                .Where(x => x.Id == storeId)
                .Include(x => x.Items).ThenInclude(x => x.Item)
                .Include(x => x.Items).ThenInclude(x => x.Item.App)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.WorkshopFile)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.WorkshopFile.Creator)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.MarketItem)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.MarketItem.Currency)
                .FirstOrDefaultAsync();

            var itemStoreDetail = _mapper.Map<SteamItemStore, ItemStoreDetailedDTO>(itemStore, this);
            if (itemStoreDetail == null)
            {
                return NotFound();
            }

            // TODO: Do this better, very lazy
            var itemStoreTaggedItems = itemStoreDetail.Items.Where(x => x.Tags != null);
            foreach (var item in itemStoreTaggedItems)
            {
                var itemType = Uri.EscapeDataString(item.ItemType ?? String.Empty);
                if (String.IsNullOrEmpty(itemType))
                {
                    continue;
                }

                var itemPrice = itemStore.Items.Select(x => x.Item).FirstOrDefault(x => x.SteamId == item.SteamId)?.Price;
                if (itemPrice == null || itemPrice <= 0)
                {
                    continue;
                }

                var marketRank = await _db.SteamApps
                    .Where(x => x.SteamId == item.SteamAppId)
                    .Select(app => new
                    {
                        Position = app.MarketItems
                            .Where(x => x.Description.Tags.Serialised.Contains(itemType))
                            .Where(x => x.BuyNowPrice < itemPrice.Value)
                            .Count(),
                        Total = app.MarketItems
                            .Where(x => x.Description.Tags.Serialised.Contains(itemType))
                            .Count() + 1,
                    })
                    .SingleOrDefaultAsync();

                if (marketRank.Total > 1)
                {
                    item.MarketRankPosition = marketRank.Position;
                    item.MarketRankTotal = marketRank.Total;
                }
            }

            return Ok(itemStoreDetail);
        }

        /// <summary>
        /// Get the next item store release date/time (estimate only)
        /// </summary>
        /// <remarks>This is an estimate only and the exact time varies from week to week. Sometimes the store can even be late by a day or two.</remarks>
        /// <returns>The expected store release date/time</returns>
        /// <response code="200">The expected date/time of the next item store release.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("nextUpdateTime")]
        [ProducesResponseType(typeof(DateTimeOffset?), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStoreNextUpdateTime()
        {
            var nextUpdateTime = await _queryProcessor.ProcessAsync(new GetStoreNextUpdateTimeRequest());
            return Ok(nextUpdateTime?.Timestamp);
        }
    }
}