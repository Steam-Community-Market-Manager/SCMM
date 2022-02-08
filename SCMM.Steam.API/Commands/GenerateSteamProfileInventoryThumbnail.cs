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

        public int ItemSize { get; set; } = 64;

        public int ItemColumns { get; set; } = 5;

        public int ItemRows { get; set; } = 5;

        public DateTimeOffset? ExpiresOn { get; set; } = null;
    }

    public class GenerateSteamProfileInventoryThumbnailResponse
    {
        public string ImageUrl { get; set; }
    }

    public class GenerateSteamProfileInventoryThumbnail : ICommandHandler<GenerateSteamProfileInventoryThumbnailRequest, GenerateSteamProfileInventoryThumbnailResponse>
    {
        private readonly SteamDbContext _db;
        private readonly IQueryProcessor _queryProcessor;
        private readonly ICommandProcessor _commandProcessor;

        public GenerateSteamProfileInventoryThumbnail(SteamDbContext db, IQueryProcessor queryProcessor, ICommandProcessor commandProcessor)
        {
            _db = db;
            _queryProcessor = queryProcessor;
            _commandProcessor = commandProcessor;
        }

        public async Task<GenerateSteamProfileInventoryThumbnailResponse> HandleAsync(GenerateSteamProfileInventoryThumbnailRequest request)
        {
            // Resolve the id
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = request.ProfileId
            });

            // TODO: Consider making these arguments in the request?
            var showDrops = false;// (resolvedId.Profile?.InventoryShowItemDrops ?? true);
            var showUnmarketable = false;// (resolvedId.Profile?.InventoryShowUnmarketableItems ?? true);
            var inventoryItemIcons = await _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => x.Description != null)
                .Where(x => showDrops || (!x.Description.IsSpecialDrop && !x.Description.IsTwitchDrop))
                .Where(x => showUnmarketable || (x.Description.IsMarketable))
                .Select(x => new
                {
                    IconUrl = x.Description.IconUrl,
                    Quantity = x.Quantity,
                    // NOTE: This isn't 100% accurate if the store item price is used. Update this to use StoreItem.Prices with the local currency
                    Value = (x.Description.MarketItem != null ? x.Description.MarketItem.SellOrderLowestPrice : (x.Description.StoreItem != null ? x.Description.StoreItem.Price ?? 0 : 0)),
                    ValueUp = (x.Description.MarketItem != null ? x.Description.MarketItem.SellOrderLowestPrice - x.Description.MarketItem.Stable24hrSellOrderLowestPrice > 0 : false),
                    ValueDown = (x.Description.MarketItem != null ? x.Description.MarketItem.SellOrderLowestPrice - x.Description.MarketItem.Stable24hrSellOrderLowestPrice < 0 : false),
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
                ImageSize = request.ItemSize,
                ImageColumns = request.ItemColumns,
                ImageRows = request.ItemRows
            });
            if (inventoryImageMosaic?.Data == null)
            {
                return null;
            }

            var uploadedInventoryImageMosaic = await _commandProcessor.ProcessWithResultAsync(new UploadImageToBlobStorageRequest()
            {
                Name = $"{request.ProfileId}-inventory-thumbnail-{request.ItemSize}x{request.ItemRows}x{request.ItemColumns}-{DateTime.UtcNow.Ticks}",
                MimeType = inventoryImageMosaic.MimeType,
                Data = inventoryImageMosaic.Data,
                ExpiresOn = request.ExpiresOn
            });
            if (uploadedInventoryImageMosaic?.ImageUrl == null)
            {
                return null;
            }

            return new GenerateSteamProfileInventoryThumbnailResponse()
            {
                ImageUrl = uploadedInventoryImageMosaic?.ImageUrl
            };
        }
    }
}
