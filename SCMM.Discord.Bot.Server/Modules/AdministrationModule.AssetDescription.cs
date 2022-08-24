using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client.Commands;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Messages;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Store;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Text.RegularExpressions;
using static Azure.Core.HttpHeader;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("import-rust-item-definitions")]
        public async Task<RuntimeResult> ImportRustAssetDescriptionAsync()
        {
            var message = await Context.Message.ReplyAsync("Importing latest item definitions...");
            var response = await _commandProcessor.ProcessWithResultAsync(new ImportSteamItemDefinitionsRequest()
            {
                AppId = Constants.RustAppId
            });

            await _steamDb.SaveChangesAsync();
            await message.ModifyAsync(
                x => x.Content = $"Imported latest item definitions from digest {response.App.ItemDefinitionsDigest} (modified: {response.App.TimeUpdated})"
            );

            return CommandResult.Success();
        }

        [Command("import-rust-asset-description")]
        public async Task<RuntimeResult> ImportRustAssetDescriptionAsync(params ulong[] classIds)
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

                await _steamDb.SaveChangesAsync();
            }

            await message.ModifyAsync(
                x => x.Content = $"Imported {classIds.Length}/{classIds.Length} asset descriptions"
            );

            return CommandResult.Success();
        }

        [Command("import-csgo-asset-description")]
        public async Task<RuntimeResult> ImportCSGOAssetDescriptionAsync(params ulong[] classIds)
        {
            var message = await Context.Message.ReplyAsync("Importing asset descriptions...");
            foreach (var classId in classIds)
            {
                await message.ModifyAsync(
                    x => x.Content = $"Importing asset description {classId} ({Array.IndexOf(classIds, classId) + 1}/{classIds.Length})..."
                );

                _ = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
                {
                    AppId = Constants.CSGOAppId,
                    AssetClassId = classId
                });

                await _steamDb.SaveChangesAsync();
            }

            await message.ModifyAsync(
                x => x.Content = $"Imported {classIds.Length}/{classIds.Length} asset descriptions"
            );

            return CommandResult.Success();
        }

        [Command("import-csgo-market-item")]
        public async Task<RuntimeResult> ImportCSGOMarketItemAsync(params string[] names)
        {
            var message = await Context.Message.ReplyAsync("Importing market items...");
            foreach (var name in names)
            {
                await message.ModifyAsync(
                    x => x.Content = $"Importing market item '{name}' ({Array.IndexOf(names, name) + 1}/{names.Length})..."
                );

                var marketListingPageHtml = await _communityClient.GetText(new SteamMarketListingPageRequest()
                {
                    AppId = Constants.CSGOAppId.ToString(),
                    MarketHashName = name,
                });

                var classIdMatchGroup = Regex.Match(marketListingPageHtml, @"\""classid\"":\""([0-9]+)\""").Groups;
                var classId = (classIdMatchGroup.Count > 1)
                    ? classIdMatchGroup[1].Value.Trim()
                    : null;

                _ = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
                {
                    AppId = Constants.CSGOAppId,
                    AssetClassId = UInt64.Parse(classId)
                });

                await _steamDb.SaveChangesAsync();
            }

            await message.ModifyAsync(
                x => x.Content = $"Imported {names.Length}/{names.Length} asset descriptions"
            );

            return CommandResult.Success();
        }

        [Command("import-item-collection-workshop-files")]
        public async Task<RuntimeResult> MissingWorkshopFiles([Remainder] string collectionName)
        {
            var message = await Context.Message.ReplyAsync("Importing item collection workshop files...");
            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_steamCfg.ApplicationKey);
            var steamRemoteStorage = steamWebInterfaceFactory.CreateSteamWebInterface<SteamRemoteStorage>();

            var apps = await _steamDb.SteamApps.ToListAsync();
            var itemCollections = await _steamDb.SteamAssetDescriptions
                .Where(x => !String.IsNullOrEmpty(x.ItemCollection) && x.CreatorId != null)
                .Where(x => !x.ItemCollection.Contains("Twitch") && !x.ItemCollection.Contains("Charitable Rust"))
                .Where(x => String.IsNullOrEmpty(collectionName) || x.ItemCollection.Contains(collectionName))
                .GroupBy(x => new { x.AppId, x.CreatorId, x.ItemCollection })
                .Select(x => x.Key)
                .ToArrayAsync();

            foreach (var itemCollection in itemCollections)
            {
                await message.ModifyAsync(
                    x => x.Content = $"Importing workshop files for item collection '{itemCollection.ItemCollection}' ({Array.IndexOf(itemCollections, itemCollection) + 1}/{itemCollections.Length})..."
                );

                var app = apps.FirstOrDefault(x => x.Id == itemCollection.AppId);
                var publishedFileIds = new List<ulong>();
                var workshopHtml = await _communityClient.GetHtml(new SteamProfileMyWorkshopFilesPageRequest()
                {
                    SteamId = itemCollection.CreatorId.ToString(),
                    AppId = app.SteamId
                });

                // Get workshop file ids
                var paginingControls = workshopHtml.Descendants("div").FirstOrDefault(x => x.Attribute("class")?.Value == "workshopBrowsePagingControls");
                var lastPageLink = paginingControls?.Descendants("a").LastOrDefault(x => x.Attribute("class")?.Value == "pagelink");
                var pages = int.Parse(lastPageLink?.Value ?? "1");
                for (int page = 1; page <= pages; page++)
                {
                    if (page != 1)
                    {
                        workshopHtml = await _communityClient.GetHtml(new SteamProfileMyWorkshopFilesPageRequest()
                        {
                            SteamId = itemCollection.CreatorId.ToString(),
                            AppId = app.SteamId,
                            Page = page
                        });
                    }

                    var workshopItems = workshopHtml.Descendants("div").Where(x => x.Attribute("class")?.Value == "workshopItem").ToList();
                    foreach (var workshopItem in workshopItems)
                    {
                        var workshopItemLink = workshopItem.Descendants("a").FirstOrDefault();
                        publishedFileIds.Add(UInt64.Parse(workshopItemLink?.Attribute("data-publishedfileid")?.Value));
                    }
                }

                // Get workshop file details
                var publishedFileDetails = await steamRemoteStorage.GetPublishedFileDetailsAsync(publishedFileIds);
                if (publishedFileDetails?.Data != null)
                {
                    var itemCollectionWords = itemCollection.ItemCollection.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    var collectionPublishedFiles = publishedFileDetails.Data
                        .Where(x => itemCollectionWords.All(w => x.Title.Contains(w, StringComparison.InvariantCultureIgnoreCase)))
                        .ToList();

                    if (collectionPublishedFiles.Any())
                    {
                        foreach (var collectionPublishedFile in collectionPublishedFiles)
                        {
                            var assetDescription = await _steamDb.SteamAssetDescriptions.FirstOrDefaultAsync(x => x.WorkshopFileId == collectionPublishedFile.PublishedFileId);
                            var workshopFile = await _steamDb.SteamWorkshopFiles.FirstOrDefaultAsync(x => x.SteamId == collectionPublishedFile.PublishedFileId.ToString());
                            workshopFile = workshopFile ?? new SteamWorkshopFile()
                            {
                                AppId = app.Id,
                                DescriptionId = assetDescription?.Id,
                                CreatorId = itemCollection.CreatorId,
                                ItemCollection = itemCollection.ItemCollection
                            };

                            workshopFile.ItemCollection = itemCollection.ItemCollection;
                            var updatedWorkshopItem = await _commandProcessor.ProcessWithResultAsync(new UpdateSteamWorkshopFileRequest()
                            {
                                WorkshopFile = workshopFile,
                                PublishedFile = collectionPublishedFile,
                                AssetDescription = assetDescription
                            });

                            if (workshopFile.IsTransient)
                            {
                                _steamDb.SteamWorkshopFiles.Add(workshopFile);
                            }
                        }
                    }
                }

                await _steamDb.SaveChangesAsync();
            }

            await message.ModifyAsync(
                x => x.Content = $"Imported {itemCollections.Length}/{itemCollections.Length} item collection workshop files"
            );

            return CommandResult.Success();
        }

        [Command("create-asset-description-collection")]
        public async Task<RuntimeResult> CreateAssetDescriptionCollectionAsync([Remainder] string collectionName)
        {
            var query = _steamDb.SteamAssetDescriptions.Where(x => x.CreatorId != null);
            foreach (var word in collectionName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                query = query.Where(x => x.Name.Contains(word));
            }

            var assetDescriptions = await query.ToListAsync();
            foreach (var assetDescriptionGroup in assetDescriptions.GroupBy(x => x.CreatorId))
            {
                if (assetDescriptionGroup.All(x => (x.ItemCollection?.Length ?? 0) < collectionName.Length))
                {
                    foreach (var assetDescription in assetDescriptionGroup)
                    {
                        assetDescription.ItemCollection = collectionName;
                    }
                }
            }

            await _steamDb.SaveChangesAsync();
            return CommandResult.Success();
        }

        [Command("delete-asset-description-collection")]
        public async Task<RuntimeResult> DeleteAssetDescriptionCollectionAsync([Remainder] string collectionName)
        {
            var assetDescriptions = await _steamDb.SteamAssetDescriptions.Where(x => x.ItemCollection == collectionName).ToListAsync();
            foreach (var assetDescription in assetDescriptions)
            {
                assetDescription.ItemCollection = null;
            }

            await _steamDb.SaveChangesAsync();
            return CommandResult.Success();
        }

        [Command("rebuild-asset-description-accepted-times")]
        public async Task<RuntimeResult> RebuildAssetDescriptionAcceptedTimesAsync()
        {
            var items = await _steamDb.SteamAssetDescriptions
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

                await _steamDb.SaveChangesAsync();
            }

            return CommandResult.Success();
        }

        [Command("tag-asset-description")]
        public async Task<RuntimeResult> TagAssetDescriptionsAsync(string tagKey, string tagValue, params ulong[] classIds)
        {
            var assetDescriptions = await _steamDb.SteamAssetDescriptions
                .Where(x => x.ClassId != null)
                .Where(x => classIds.Contains(x.ClassId.Value))
                .ToListAsync();
            foreach (var assetDescription in assetDescriptions)
            {
                assetDescription.Tags = new PersistableStringDictionary(assetDescription.Tags)
                {
                    [tagKey] = tagValue
                };
            }

            await _steamDb.SaveChangesAsync();
            return CommandResult.Success();
        }

        [Command("untag-asset-description")]
        public async Task<RuntimeResult> UntagAssetDescriptionsAsync(string tagKey, params ulong[] classIds)
        {
            var assetDescriptions = await _steamDb.SteamAssetDescriptions
                .Where(x => x.ClassId != null)
                .Where(x => classIds.Contains(x.ClassId.Value))
                .ToListAsync();
            foreach (var assetDescription in assetDescriptions)
            {
                assetDescription.Tags = new PersistableStringDictionary(assetDescription.Tags);
                assetDescription.Tags.Remove(tagKey);
            }

            await _steamDb.SaveChangesAsync();
            return CommandResult.Success();
        }

        [Command("add-asset-description-note")]
        public async Task<RuntimeResult> AddAssetDescriptionNoteAsync(ulong classId, [Remainder] string note)
        {
            var assetDescription = await _steamDb.SteamAssetDescriptions.FirstOrDefaultAsync(x => classId == x.ClassId);
            if (assetDescription != null)
            {
                assetDescription.Notes = new PersistableStringCollection(assetDescription.Notes)
                {
                    note
                };

                await _steamDb.SaveChangesAsync();
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
            var assetDescription = await _steamDb.SteamAssetDescriptions.FirstOrDefaultAsync(x => classId == x.ClassId);
            if (assetDescription != null)
            {
                assetDescription.Notes = new PersistableStringCollection(assetDescription.Notes);
                assetDescription.Notes.Remove(assetDescription.Notes.ElementAt(index));

                await _steamDb.SaveChangesAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Fail("Unable to find asset description with the specific class id");
            }
        }

        [Command("download-workshop-files")]
        public async Task<RuntimeResult> DownloadAssetDescriptionWorkshopFiles(params ulong[] classIds)
        {
            var workshopFiles = await _steamDb.SteamAssetDescriptions
                .Where(x => x.ClassId != null && (classIds.Length == 0 || classIds.Contains(x.ClassId.Value)))
                .Where(x => x.WorkshopFileId != null)
                .Select(x => new
                {
                    AppId = x.App.SteamId,
                    WorkshopFileId = x.WorkshopFileId.Value
                })
                .ToListAsync();

            var messages = new List<DownloadSteamWorkshopFileMessage>();
            foreach (var workshopFile in workshopFiles)
            {
                messages.Add(new DownloadSteamWorkshopFileMessage()
                {
                    AppId = UInt64.Parse(workshopFile.AppId),
                    PublishedFileId = workshopFile.WorkshopFileId,
                    Force = true
                });
            }

            await _serviceBusClient.SendMessagesAsync(messages);
            return CommandResult.Success();
        }

        [Command("reanalyse-workshop-files")]
        public async Task<RuntimeResult> ReanalyseAssetDescriptionWorkshopFiles(params ulong[] classIds)
        {
            var workshopFileUrls = await _steamDb.SteamAssetDescriptions
                .Where(x => x.ClassId != null && (classIds.Length == 0 || classIds.Contains(x.ClassId.Value)))
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
