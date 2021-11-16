using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Store;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.API.Commands
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
        public FileData Image { get; set; }
    }

    public class GenerateSteamProfileInventoryThumbnail : ICommandHandler<GenerateSteamProfileInventoryThumbnailRequest, GenerateSteamProfileInventoryThumbnailResponse>
    {
        private readonly SteamDbContext _db;
        private readonly IQueryProcessor _queryProcessor;

        public GenerateSteamProfileInventoryThumbnail(SteamDbContext db, IQueryProcessor queryProcessor)
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

            var showDrops = (resolvedId.Profile?.ShowItemDrops ?? false);
            var inventoryItemIcons = await _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => x.Description != null)
                .Where(x => showDrops || (!x.Description.IsSpecialDrop && !x.Description.IsTwitchDrop))
                .Select(x => new
                {
                    IconUrl = x.Description.IconUrl,
                    Quantity = x.Quantity,
                    // NOTE: This isn't 100% accurate if the store item price is used. Update this to use StoreItem.Prices with the local currency
                    Value = (x.Description.MarketItem != null ? x.Description.MarketItem.LastSaleValue : (x.Description.StoreItem != null ? x.Description.StoreItem.Price ?? 0 : 0)),
                    ValueUp = (x.Description.MarketItem != null ? x.Description.MarketItem.LastSaleValue - x.Description.MarketItem.Stable24hrValue > 0 : false),
                    ValueDown = (x.Description.MarketItem != null ? x.Description.MarketItem.LastSaleValue - x.Description.MarketItem.Stable24hrValue < 0 : false),
                    Banned = x.Description.IsBanned
                })
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            var inventoryImageSources = new List<ImageSource>();
            foreach (var inventoryItemIcon in inventoryItemIcons)
            {
                if (!inventoryImageSources.Any(x => x.ImageUrl == inventoryItemIcon.IconUrl))
                {
                    var symbol = ImageSymbol.None;
                    if (inventoryItemIcon.Banned)
                    {
                        symbol = ImageSymbol.Cross;
                    }
                    else if (inventoryItemIcon.ValueUp)
                    {
                        symbol = ImageSymbol.ChevronUp;
                    }
                    else if (inventoryItemIcon.ValueDown)
                    {
                        symbol = ImageSymbol.ChevronDown;
                    }
                    inventoryImageSources.Add(new ImageSource()
                    {
                        ImageUrl = inventoryItemIcon.IconUrl,
                        Badge = inventoryItemIcons.Where(x => x.IconUrl == inventoryItemIcon.IconUrl).Sum(x => x.Quantity),
                        Symbol = symbol
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
            if (inventoryImageMosaic?.Data == null)
            {
                return null;
            }

            var imageData = new FileData()
            {
                MimeType = inventoryImageMosaic.MimeType,
                Data = inventoryImageMosaic.Data,
                ExpiresOn = request.ExpiresOn
            };

            _db.FileData.Add(imageData);

            return new GenerateSteamProfileInventoryThumbnailResponse()
            {
                Image = imageData
            };
        }
    }
}
