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
            var minDate = DateTimeOffset.MinValue;
            var assetDescriptions = await _db.SteamAssetDescriptions
                .Select(x => new
                {
                    AssetDescription = x,
                    TimeAccepted = (x.MarketItem != null 
                        ? x.MarketItem.SalesHistory.Min(x => x.Timestamp).Subtract(TimeSpan.FromDays(7)) 
                        : x.TimeAccepted ?? minDate
                    )
                })
                .ToListAsync();

            var culture = CultureInfo.InvariantCulture;
            var groupedReleases = assetDescriptions
                .Where(x => x.TimeAccepted > minDate)
                .GroupBy(
                    x => new Tuple<int, int>(
                        x.TimeAccepted.UtcDateTime.Year,
                        culture.Calendar.GetWeekOfYear(x.TimeAccepted.UtcDateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)
                    )
                );

            var debug = new StringBuilder();
            var stores = new Dictionary<DateTimeOffset, IList<SteamAssetDescription>>();
            foreach (var groupedRelease in groupedReleases.OrderBy(x => x.Key.Item1).ThenBy(x => x.Key.Item2))
            {
                var storeProbablyStartedOn = groupedRelease.Min(x => x.TimeAccepted);
                foreach (var item in groupedRelease)
                {
                    if ((item.TimeAccepted - storeProbablyStartedOn) >= TimeSpan.FromDays(2))
                    {
                        storeProbablyStartedOn = item.TimeAccepted;
                    }
                    if (!stores.ContainsKey(storeProbablyStartedOn))
                    {
                        stores[storeProbablyStartedOn] = new List<SteamAssetDescription>();
                    }
                    stores[storeProbablyStartedOn].Add(item.AssetDescription);
                }
            }

            foreach (var store in stores)
            {
                debug.AppendLine($"{store.Key.ToString("yyyy MMM d")}: ({store.Value.Count()} items)");
                foreach (var item in store.Value)
                {
                    debug.AppendLine($"\t - {item.Name} ({store.Key})");
                    if (item.TimeAccepted == null)
                    {
                        item.TimeAccepted = store.Key;
                    }
                }
            }

            var debugStoreListText = debug.ToString();

            await _db.SaveChangesAsync();
            return CommandResult.Success();
        }
    }
}
