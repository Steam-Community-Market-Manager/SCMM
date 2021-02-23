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
        public string ProfileId { get; set; }

        public int TileSize { get; set; } = 128;

        public int Columns { get; set; } = 5;

        public int? Rows { get; set; } = 5;

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
            // Resolve the id
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = request.ProfileId
            });

            var inventoryItemIcons = _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.ProfileId == resolvedId.Id)
                .Where(x => x.Description != null)
                .Select(x => new
                {
                    IconUrl = x.Description.IconUrl,
                    Value = (x.Description.MarketItem != null ? x.Description.MarketItem.Last1hrValue : (x.Description.StoreItem != null ? x.Description.StoreItem.Price : 0)),
                    ValueUp = (x.Description.MarketItem != null ? x.Description.MarketItem.Last48hrValue - x.Description.MarketItem.Last24hrValue > 0 : false),
                    ValueDown = (x.Description.MarketItem != null ? x.Description.MarketItem.Last48hrValue - x.Description.MarketItem.Last24hrValue < 0 : false)
                })
                .OrderByDescending(x => x.Value)
                .ToList();

            var inventoryImageSources = new List<ImageSource>();
            foreach (var inventoryItemIcon in inventoryItemIcons)
            {
                if (!inventoryImageSources.Any(x => x.ImageUrl == inventoryItemIcon.IconUrl))
                {
                    inventoryImageSources.Add(new ImageSource()
                    {
                        ImageUrl = inventoryItemIcon.IconUrl,
                        Badge = inventoryItemIcons.Count(x => x.IconUrl == inventoryItemIcon.IconUrl),
                        ChevronUp = inventoryItemIcon.ValueUp,
                        ChevronDown = inventoryItemIcon.ValueDown
                    });
                }
            }

            var inventoryImageMosaic = await _queryProcessor.ProcessAsync(new GetImageMosaicRequest()
            {
                ImageSources = inventoryImageSources,
                TileSize = request.TileSize,
                Columns = request.Columns,
                Rows = request.Rows
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
