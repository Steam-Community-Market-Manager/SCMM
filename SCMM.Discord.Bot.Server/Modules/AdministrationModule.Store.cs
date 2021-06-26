using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("update-store-name")]
        public async Task<RuntimeResult> UpdateStoreNameAsync(DateTime storeDate, [Remainder] string storeName)
        {
            var itemStore = await _db.SteamItemStores
                .OrderByDescending(x => x.Start)
                .Where(x => storeDate >= x.Start)
                .FirstOrDefaultAsync();

            if (itemStore != null)
            {
                itemStore.Name = storeName;
                await _db.SaveChangesAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Fail("Unable to find a store for the specific date");
            }
        }

        [Command("add-store-media")]
        public async Task<RuntimeResult> AddStoreMediaAsync(DateTime storeDate, [Remainder] string media)
        {
            var itemStore = await _db.SteamItemStores
                .OrderByDescending(x => x.Start)
                .Where(x => storeDate >= x.Start)
                .FirstOrDefaultAsync();

            if (itemStore != null)
            {
                itemStore.Media.Add(media);
                await _db.SaveChangesAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Fail("Unable to find a store for the specific date");
            }
        }

        [Command("remove-store-media")]
        public async Task<RuntimeResult> RemoveStoreMediaAsync(DateTime storeDate, [Remainder] string media)
        {
            var itemStore = await _db.SteamItemStores
                .OrderByDescending(x => x.Start)
                .Where(x => storeDate >= x.Start)
                .FirstOrDefaultAsync();

            if (itemStore != null)
            {
                itemStore.Media.Remove(media);
                await _db.SaveChangesAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Fail("Unable to find a store for the specific date");
            }
        }

        /*
        [Command("rebuild-store-list")]
        public async Task<RuntimeResult> RebuildStoreListAsync()
        {
            var assetDescriptions = await _db.SteamAssetDescriptions
                .Where(x => x.TimeAccepted != null)
                .Include(x => x.App)
                .Include(x => x.StoreItem).ThenInclude(x => x.Stores)
                .ToListAsync();

            var assetDescriptionStoreGroups = assetDescriptions.GroupBy(x => x.TimeAccepted.Value.Date).OrderBy(x => x.Key);
            foreach (var assetDescriptionStoreGroup in assetDescriptionStoreGroups.Where(x => x.Any()))
            {
                // Ensure the store item exists
                foreach (var assetDescription in assetDescriptionStoreGroup)
                {
                    if (assetDescription.StoreItem == null)
                    {
                        _db.SteamStoreItems.Add(
                            assetDescription.StoreItem = new SteamStoreItem()
                            {
                                SteamId = null, // ???
                                App = assetDescription.App,
                                Description = assetDescription
                            }
                        );
                    }
                }

                // Ensure the item store exists
                var storeStart = assetDescriptionStoreGroup.Key;
                var store = _db.SteamItemStores.FirstOrDefault(x => x.Start.Date == storeStart) ??
                            _db.SteamItemStores.Local.FirstOrDefault(x => x.Start.Date == storeStart);
                if (store == null)
                {
                    _db.SteamItemStores.Add(
                        store = new SteamItemStore()
                        {
                            App = assetDescriptionStoreGroup.FirstOrDefault(x => x.App != null)?.App,
                            Start = storeStart,
                            End = storeStart.AddDays(7),
                            IsDraft = true
                        }
                    );
                }

                // Link the store item to the item store (if missing)
                foreach (var assetDescription in assetDescriptionStoreGroup)
                {
                    var storeItemLink = assetDescription.StoreItem.Stores.FirstOrDefault(x => x.Store == store);
                    if (storeItemLink == null)
                    {
                        store.Items.Add(new SteamStoreItemItemStore()
                        {
                            Store = store,
                            Item = assetDescription.StoreItem,
                            IsDraft = true
                        });
                    }
                }
            }

            await _db.SaveChangesAsync();
            return CommandResult.Success();
        }
        */

        [Command("import-tgg-stores")]
        public async Task<RuntimeResult> ImportTGGStoresAsync()
        {
            var start = new DateTimeOffset(new DateTime(2017, 01, 01), TimeZoneInfo.Local.BaseUtcOffset);
            var app = await _db.SteamApps.FirstOrDefaultAsync(x => x.SteamId == Constants.RustAppId.ToString());
            var existingStores = await _db.SteamItemStores.ToListAsync();

            do
            {
                var videos = await _googleClient.SearchVideosAsync(
                    channelId: "UCvCBuwbtKRwM0qMi7rc7CUw", // TGG
                    query: "Rust Skins",
                    publishedAfter: start.DateTime,
                    publishedBefore: start.DateTime.AddDays(365),
                    maxResults: 50
                );

                var rustSkinVideos = videos
                    .Where(x => x.Title.Contains("Rust"))
                    .Where(x =>
                        !x.Title.Contains("Barrel") && !x.Title.Contains("Contest") && !x.Title.Contains("Winners") && !x.Title.Contains("Tutorials") && !x.Title.Contains("Coming") &&
                        !Regex.IsMatch(x.Title, @"^.*Top.*\|") && !Regex.IsMatch(x.Title, @"^.*Picks.*\|") && !Regex.IsMatch(x.Title, @"^.*Twitch.*\|")
                    )
                    .OrderBy(x => x.PublishedAt)
                    .ToList();

                if (!rustSkinVideos.Any())
                {
                    break;
                }

                foreach (var video in rustSkinVideos)
                {
                    var nextVideo = rustSkinVideos.Skip(rustSkinVideos.IndexOf(video) + 1).FirstOrDefault();
                    var storeName = Regex.Match(video.Title, @"\|\s([^\(]*)").Groups.OfType<Group>().Skip(1).FirstOrDefault()?.Value;
                    var storeStart = video.PublishedAt.Value;
                    var storeEnd = (nextVideo != null ? nextVideo.PublishedAt.Value : storeStart.AddDays(7));
                    var store = existingStores
                        .Where(x => storeStart >= x.Start)
                        .OrderBy(x => x.Start)
                        .FirstOrDefault();

                    if (store != null)
                    {
                        if (store.Name == null)
                        {
                            store.Name = video.Title;
                        }
                        if (!store.Media.Contains(video.Id))
                        {
                            store.Media.Add(video.Id);
                        }
                    }
                    else
                    {
                        _db.SteamItemStores.Add(
                            new SteamItemStore()
                            {
                                App = app,
                                Name = storeName,
                                Start = storeStart,
                                End = storeEnd,
                                Media = new PersistableStringCollection()
                                {
                                    video.Id
                                },
                                IsDraft = true
                            }
                        );
                    }
                }

                start = rustSkinVideos.Max(x => x.PublishedAt.Value).AddDays(1);

            } while (start < DateTime.UtcNow);

            await _db.SaveChangesAsync();
            return CommandResult.Success();
        }

        // store.steampowered.com/itemstore/252490/
        // store.steampowered.com/itemstore/252490/?filter=All
        [Command("import-web-archive-store-prices")]
        public async Task<RuntimeResult> ImportWebArchiveStorePricesAsync(string itemStoreUrl)
        {
            using var client = new HttpClient();
            var webArchiveSnapshotResponse = await client.GetAsync(
                $"https://web.archive.org/cdx/search/cdx?url={Uri.EscapeUriString(itemStoreUrl)}"
            );

            webArchiveSnapshotResponse?.EnsureSuccessStatusCode();

            var webArchiveSnapshotResponseText = await webArchiveSnapshotResponse?.Content?.ReadAsStringAsync();
            if (String.IsNullOrEmpty(webArchiveSnapshotResponseText))
            {
                return CommandResult.Fail("No web archive snapshots found");
            }

            var message = await Context.Message.ReplyAsync("Importing snapshots...");
            var webArchiveSnapshots = webArchiveSnapshotResponseText.Split('\n');
            foreach (var webArchiveSnapshot in webArchiveSnapshots)
            {
                // "com,example)/ 20020120142510 http://example.com:80/ text/html 200 HT2DYGA5UKZCPBSFVCV3JOBXGW2G5UUA 1792"
                var snapshot = webArchiveSnapshot.Split(' ', StringSplitOptions.TrimEntries).Skip(1).FirstOrDefault();
                await message.ModifyAsync(
                    x => x.Content = $"Importing snapshot {snapshot} ({Array.IndexOf(webArchiveSnapshots, webArchiveSnapshot) + 1}/{webArchiveSnapshots.Length})..."
                );

                try
                {
                    await _commandProcessor.ProcessAsync(new ImportSteamItemStorePricesRequest()
                    {
                        ItemStoreUrl = $"http://web.archive.org/web/{snapshot}/{Uri.EscapeUriString(itemStoreUrl)}"
                    });

                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    await Context.Message.ReplyAsync(
                        $"Error importing snapshot {snapshot}, skipping. {ex.Message}"
                    );
                    continue;
                }
            }

            await _db.SaveChangesAsync();
            await message.ModifyAsync(
                x => x.Content = $"Imported {webArchiveSnapshots.Length}/{webArchiveSnapshots.Length} snapshots"
            );

            return CommandResult.Success();
        }
    }
}
