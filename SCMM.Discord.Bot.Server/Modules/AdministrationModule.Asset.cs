using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("import-asset")]
        public async Task<RuntimeResult> ImportAssetAsync(params ulong[] assetClassIds)
        {
            foreach (var assetClassId in assetClassIds)
            {
                _ = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
                {
                    AppId = Constants.RustAppId,
                    AssetClassId = assetClassId
                });
            }

            await _db.SaveChangesAsync();
            return CommandResult.Success();
        }

        [Command("create-asset-collection")]
        public async Task<RuntimeResult> CreateAssetCollectionAsync([Remainder] string collectionName)
        {
            var query = _db.SteamAssetDescriptions.Where(x => x.CreatorId != null);
            foreach (var word in collectionName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                query = query.Where(x => x.Name.Contains(word));
            }

            var assetDescriptions = await query.ToListAsync();
            foreach (var assetDescriptionGroup in assetDescriptions.GroupBy(x => x.CreatorId))
            {
                if (assetDescriptionGroup.Count() > 1)
                {
                    foreach (var assetDescription in assetDescriptionGroup)
                    {
                        assetDescription.ItemCollection = collectionName;
                    }
                }
            }

            await _db.SaveChangesAsync();
            return CommandResult.Success();
        }

        [Command("delete-asset-collection")]
        public async Task<RuntimeResult> DeleteAssetCollectionAsync([Remainder] string collectionName)
        {
            var assetDescriptions = await _db.SteamAssetDescriptions.Where(x => x.ItemCollection == collectionName).ToListAsync();
            foreach (var assetDescription in assetDescriptions)
            {
                assetDescription.ItemCollection = null;
            }

            await _db.SaveChangesAsync();
            return CommandResult.Success();
        }

        [Command("rebuild-asset-collection")]
        public async Task<RuntimeResult> RebuildAssetCollectionAsync()
        {
            var assetDescriptions = await _db.SteamAssetDescriptions.ToListAsync();

            // Reset item collections
            foreach (var assetDescription in assetDescriptions)
            {
                assetDescription.ItemCollection = null;
            }
            await _db.SaveChangesAsync();

            // Rebuild item collections (batch it, can take a while...)
            foreach (var assetDescriptionBatch in assetDescriptions.Batch(100))
            {
                foreach (var assetDescription in assetDescriptionBatch)
                {
                    _ = await _commandProcessor.ProcessWithResultAsync(new UpdateSteamAssetDescriptionRequest()
                    {
                        AssetDescription = assetDescription
                    });
                }
                await _db.SaveChangesAsync();
            }

            return CommandResult.Success();
        }

        [Command("rebuild-asset-accepted-times")]
        public async Task<RuntimeResult> RebuildAssetAcceptedTimesAsync()
        {
            var assetDescriptions = await _db.SteamAssetDescriptions
                .Select(x => new
                {
                    AssetDescription = x,
                    TimeAccepted = (x.MarketItem != null 
                        ? x.MarketItem.SalesHistory.Min(x => x.Timestamp) 
                        : x.TimeAccepted
                    )
                })
                .ToListAsync();

            // Group items in to stores where they were accepted within 3 days or less than eachother
            var stores = new Dictionary<DateTimeOffset, IList<SteamAssetDescription>>();
            var filteredAssetDescriptions = assetDescriptions.Where(x => x.TimeAccepted != null);
            var storeProbablyStartedOn = filteredAssetDescriptions.Min(x => x.TimeAccepted);
            foreach (var item in filteredAssetDescriptions.OrderBy(x => x.TimeAccepted))
            {
                if ((item.TimeAccepted - storeProbablyStartedOn) > TimeSpan.FromDays(3))
                {
                    storeProbablyStartedOn = item.TimeAccepted;
                }
                if (!stores.ContainsKey(storeProbablyStartedOn.Value))
                {
                    stores[storeProbablyStartedOn.Value] = new List<SteamAssetDescription>();
                }
                stores[storeProbablyStartedOn.Value].Add(item.AssetDescription);
            }

            // Merge item stores containing less than three items in to the closest store
            foreach (var store in stores)
            {
                //...
            }
            /*
            var videos = await _googleClient.SearchVideosAsync(
                query: String.Empty,
                channelId: "UCvCBuwbtKRwM0qMi7rc7CUw",
                publishedAfter: filteredAssetDescriptions.Min(x => x.TimeAccepted).Value.UtcDateTime,
                maxResults: Int32.MaxValue
            );
            var filteredVideos = videos?
                .Where(x => x.Title.Contains("Rust Skins", StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(x => x.PublishedAt);
            */

            var debug = new StringBuilder();
            foreach (var store in stores)
            {
                debug.AppendLine();
                debug.AppendLine($"{store.Key.ToString("yyyy MMM d")}: ({store.Value.Count()} items)");
                /*
                var storeVideo = filteredVideos?
                    .Where(x => x.PublishedAt >= store.Key)
                    .FirstOrDefault();
                if (storeVideo != null)
                {
                    debug.AppendLine($"{storeVideo.Title} ({new DateTimeOffset(storeVideo.PublishedAt.Value)})");
                    debug.AppendLine($"https://www.youtube.com/watch?v={storeVideo.Id}");
                }
                */
                foreach (var item in store.Value)
                {
                    debug.AppendLine($"\t - {item.Name} ({assetDescriptions.FirstOrDefault(x => x.AssetDescription == item)?.TimeAccepted})");
                    item.TimeAccepted = store.Key;
                }
            }
            var debugStoreListText = debug.ToString();

            //await _db.SaveChangesAsync();
            return CommandResult.Success();
        }
    }
}
