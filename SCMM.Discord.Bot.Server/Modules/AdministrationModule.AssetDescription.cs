using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client.Commands;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Messages;
using SCMM.Steam.Data.Models;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("import-item-definitions")]
        public async Task<RuntimeResult> ImportAssetDescriptionAsync()
        {
            var message = await Context.Message.ReplyAsync("Importing latest item definitions...");
            var response = await _commandProcessor.ProcessWithResultAsync(new ImportSteamItemDefinitionsRequest()
            {
                AppId = Constants.RustAppId
            });

            await _db.SaveChangesAsync();
            await message.ModifyAsync(
                x => x.Content = $"Imported latest item definitions from digest {response.App.ItemDefinitionsDigest} (modified: {response.App.TimeUpdated})"
            );

            return CommandResult.Success();
        }

        [Command("import-asset-description")]
        public async Task<RuntimeResult> ImportAssetDescriptionAsync(params ulong[] classIds)
        {
            var message = await Context.Message.ReplyAsync("Importing asset descriptions...");
            foreach (var classId in classIds)
            {
                await message.ModifyAsync(
                    x => x.Content = $"Importing asset description {classId} ({Array.IndexOf(classIds, classId) + 1}/{classIds.Length})..."
                );

                _ = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
                {
                    AppId = Constants.RustAppId,
                    AssetClassId = classId
                });

                await _db.SaveChangesAsync();
            }

            await message.ModifyAsync(
                x => x.Content = $"Imported {classIds.Length}/{classIds.Length} asset descriptions"
            );

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

        [Command("rebuild-asset-description-accepted-times")]
        public async Task<RuntimeResult> RebuildAssetDescriptionAcceptedTimesAsync()
        {
            var items = await _db.SteamAssetDescriptions
                .Select(x => new
                {
                    AssetDescription = x,
                    TimeAccepted = x.TimeAccepted,
                    TimeStore = (x.StoreItem != null ? (DateTimeOffset?)x.StoreItem.Stores.Where(x => x.Store.Start != null).Min(y => y.Store.Start) : null),
                    TimeMarket = (x.MarketItem != null ? (DateTimeOffset?)x.MarketItem.SalesHistory.Min(y => y.Timestamp) : null)
                })
                .ToListAsync();

            // Rebuild item accepted times
            foreach (var batch in items.Batch(100))
            {
                foreach (var item in batch)
                {
                    // Always use the earliest possible date that we know of
                    var earliestTime = item.TimeAccepted;
                    if (item.TimeStore != null && (item.TimeStore < earliestTime || earliestTime == null))
                    {
                        earliestTime = item.TimeStore;
                    }
                    if (item.TimeMarket != null && (item.TimeMarket < earliestTime || earliestTime == null))
                    {
                        earliestTime = item.TimeMarket;
                    }

                    if (item.AssetDescription.TimeAccepted == null || item.AssetDescription.TimeAccepted > earliestTime)
                    {
                        item.AssetDescription.TimeAccepted = earliestTime;
                        item.AssetDescription.IsAccepted = true;
                    }
                }

                await _db.SaveChangesAsync();
            }

            return CommandResult.Success();
        }

        [Command("tag-asset-description")]
        public async Task<RuntimeResult> TagAssetDescriptionsAsync(string tagKey, string tagValue, params ulong[] classIds)
        {
            var assetDescriptions = await _db.SteamAssetDescriptions.Where(x => classIds.Contains(x.ClassId)).ToListAsync();
            foreach (var assetDescription in assetDescriptions)
            {
                assetDescription.Tags = new PersistableStringDictionary(assetDescription.Tags)
                {
                    [tagKey] = tagValue
                };
            }

            await _db.SaveChangesAsync();
            return CommandResult.Success();
        }

        [Command("untag-asset-description")]
        public async Task<RuntimeResult> UntagAssetDescriptionsAsync(string tagKey, params ulong[] classIds)
        {
            var assetDescriptions = await _db.SteamAssetDescriptions.Where(x => classIds.Contains(x.ClassId)).ToListAsync();
            foreach (var assetDescription in assetDescriptions)
            {
                assetDescription.Tags = new PersistableStringDictionary(assetDescription.Tags);
                assetDescription.Tags.Remove(tagKey);
            }

            await _db.SaveChangesAsync();
            return CommandResult.Success();
        }

        [Command("add-asset-description-note")]
        public async Task<RuntimeResult> AddAssetDescriptionNoteAsync(ulong classId, [Remainder] string note)
        {
            var assetDescription = await _db.SteamAssetDescriptions.FirstOrDefaultAsync(x => classId == x.ClassId);
            if (assetDescription != null)
            {
                assetDescription.Notes = new PersistableStringCollection(assetDescription.Notes)
                {
                    note
                };

                await _db.SaveChangesAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Fail("Unable to find asset description with the specific class id");
            }
        }

        [Command("remove-asset-description-note")]
        public async Task<RuntimeResult> RemoveAssetDescriptionNoteAsync(ulong classId, int index = 0)
        {
            var assetDescription = await _db.SteamAssetDescriptions.FirstOrDefaultAsync(x => classId == x.ClassId);
            if (assetDescription != null)
            {
                assetDescription.Notes = new PersistableStringCollection(assetDescription.Notes);
                assetDescription.Notes.Remove(assetDescription.Notes.ElementAt(index));

                await _db.SaveChangesAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Fail("Unable to find asset description with the specific class id");
            }
        }

        [Command("reanalyse-workshop-files")]
        public async Task<RuntimeResult> ReanalyseAssetDescriptionWorkshopFiles(params ulong[] classIds)
        {
            var workshopFileUrls = await _db.SteamAssetDescriptions
                .Where(x => classIds.Length == 0 || classIds.Contains(x.ClassId))
                .Where(x => x.WorkshopFileId != null && !string.IsNullOrEmpty(x.WorkshopFileUrl))
                .Select(x => x.WorkshopFileUrl)
                .ToListAsync();

            var messages = new List<AnalyseSteamWorkshopFileMessage>();
            foreach (var workshopFileUrl in workshopFileUrls)
            {
                messages.Add(new AnalyseSteamWorkshopFileMessage()
                {
                    BlobName = workshopFileUrl.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault(),
                    Force = true
                });
            }

            await _serviceBusClient.SendMessagesAsync(messages);
            return CommandResult.Success();
        }

    }
}
