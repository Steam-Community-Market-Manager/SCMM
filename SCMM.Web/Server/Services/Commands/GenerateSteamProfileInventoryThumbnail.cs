using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models;
using SCMM.Web.Server.Services.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Commands
{
    public class GenerateSteamProfileInventoryThumbnailRequest : ICommand<GenerateSteamProfileInventoryThumbnailResponse>
    {
        public string SteamId { get; set; }

        public DateTimeOffset? ExpiresOn { get; set; } = null;
    }

    public class GenerateSteamProfileInventoryThumbnailResponse
    {
        public ImageData Image { get; set; }
    }

    public class GenerateSteamProfileInventoryThumbnail : ICommandHandler<GenerateSteamProfileInventoryThumbnailRequest, GenerateSteamProfileInventoryThumbnailResponse>
    {
        private readonly ScmmDbContext _db;
        private readonly IQueryProcessor _queryProcessor;

        public GenerateSteamProfileInventoryThumbnail(ScmmDbContext db, IQueryProcessor queryProcessor)
        {
            _db = db;
            _queryProcessor = queryProcessor;
        }

        public async Task<GenerateSteamProfileInventoryThumbnailResponse> HandleAsync(GenerateSteamProfileInventoryThumbnailRequest request)
        {
            var steamId = request.SteamId;
            if (String.IsNullOrEmpty(steamId))
            {
                return null;
            }

            var inventoryItemIcons = _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.Profile.SteamId == steamId || x.Profile.ProfileId == steamId)
                .Where(x => x.Description != null)
                .Select(x => new
                {
                    IconUrl = x.Description.IconUrl,
                    Value = (x.Description.MarketItem != null ? x.Description.MarketItem.Last1hrValue : (x.Description.StoreItem != null ? x.Description.StoreItem.Price : 0))
                })
                .OrderByDescending(x => x.Value)
                .Select(x => x.IconUrl)
                .ToList();

            var inventoryImageSources = new List<ImageSource>();
            foreach (var inventoryItemIcon in inventoryItemIcons)
            {
                if (!inventoryImageSources.Any(x => x.ImageUrl == inventoryItemIcon))
                {
                    inventoryImageSources.Add(new ImageSource()
                    {
                        ImageUrl = inventoryItemIcon,
                        Badge = inventoryItemIcons.Count(x => x == inventoryItemIcon)
                    });
                }
            }

            var inventoryImageMosaic = await _queryProcessor.ProcessAsync(new GetImageMosaicRequest()
            {
                ImageSources = inventoryImageSources,
                TileSize = 128,
                Columns = 5,
                Rows = 5
            });

            var imageData = new ImageData()
            {
                Data = inventoryImageMosaic.Data,
                MimeType = inventoryImageMosaic.MimeType,
                ExpiresOn = request.ExpiresOn
            };

            _db.ImageData.Add(imageData);

            return new GenerateSteamProfileInventoryThumbnailResponse()
            {
                Image = imageData
            };
        }
    }
}
