﻿using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("import-asset-description")]
        public async Task<RuntimeResult> ImportAssetDescriptionAsync(params ulong[] assetClassIds)
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

        [Command("create-asset-description-collection")]
        public async Task<RuntimeResult> CreateAssetDescriptionCollectionAsync([Remainder] string collectionName)
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

        [Command("delete-asset-description-collection")]
        public async Task<RuntimeResult> DeleteAssetDescriptionCollectionAsync([Remainder] string collectionName)
        {
            var assetDescriptions = await _db.SteamAssetDescriptions.Where(x => x.ItemCollection == collectionName).ToListAsync();
            foreach (var assetDescription in assetDescriptions)
            {
                assetDescription.ItemCollection = null;
            }

            await _db.SaveChangesAsync();
            return CommandResult.Success();
        }

        [Command("rebuild-asset-description-collections")]
        public async Task<RuntimeResult> RebuildAssetDescriptionCollectionsAsync()
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

        [Command("rebuild-asset-description-accepted-times")]
        public async Task<RuntimeResult> RebuildAssetDescriptionAcceptedTimesAsync()
        {
            var items = await _db.SteamAssetDescriptions
                .Select(x => new
                {
                    AssetDescription = x,
                    TimeAccepted = (x.MarketItem != null 
                        ? x.MarketItem.SalesHistory.Min(x => x.Timestamp) // the earliest date they appeared on the market
                        : x.TimeAccepted // the date we saw them get accepted on the workshop
                    )
                })
                .ToListAsync();

            foreach (var item in items)
            {
                // Use the earliest date we know about
                if (item.TimeAccepted < item.AssetDescription.TimeAccepted || item.AssetDescription.TimeAccepted == null)
                {
                    item.AssetDescription.TimeAccepted = item.TimeAccepted;
                }
            }
            
            await _db.SaveChangesAsync();
            return CommandResult.Success();
        }
    }
}