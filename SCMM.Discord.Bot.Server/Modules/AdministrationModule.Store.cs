using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("set-store-name")]
        public async Task<RuntimeResult> SetStoreNameAsync(DateTime storeDate, [Remainder] string storeName)
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
        public async Task<RuntimeResult> AddStoreMediaAsync(DateTime storeDate, params string[] media)
        {
            var itemStore = await _db.SteamItemStores
                .OrderByDescending(x => x.Start)
                .Where(x => storeDate >= x.Start)
                .FirstOrDefaultAsync();

            if (itemStore != null)
            {
                itemStore.Media = new PersistableStringCollection(itemStore.Media);
                foreach (var item in media)
                {
                    if (!itemStore.Media.Contains(item))
                    {
                        itemStore.Media.Add(item);
                    }
                }

                await _db.SaveChangesAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Fail("Unable to find a store for the specific date");
            }
        }

        [Command("remove-store-media")]
        public async Task<RuntimeResult> RemoveStoreMediaAsync(DateTime storeDate, params string[] media)
        {
            var itemStore = await _db.SteamItemStores
                .OrderByDescending(x => x.Start)
                .Where(x => storeDate >= x.Start)
                .FirstOrDefaultAsync();

            if (itemStore != null)
            {
                itemStore.Media = new PersistableStringCollection(itemStore.Media);
                foreach (var item in media)
                {
                    if (itemStore.Media.Contains(item))
                    {
                        itemStore.Media.Remove(item);
                    }
                }

                await _db.SaveChangesAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Fail("Unable to find a store for the specific date");
            }
        }

        [Command("add-store-note")]
        public async Task<RuntimeResult> AddStoreNoteAsync(DateTime storeDate, [Remainder] string note)
        {
            var itemStore = await _db.SteamItemStores
                .OrderByDescending(x => x.Start)
                .Where(x => storeDate >= x.Start)
                .FirstOrDefaultAsync();

            if (itemStore != null)
            {
                itemStore.Notes = new PersistableStringCollection(itemStore.Notes)
                {
                    note
                };

                await _db.SaveChangesAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Fail("Unable to find a store for the specific date");
            }
        }

        [Command("remove-store-note")]
        public async Task<RuntimeResult> RemoveStoreNoteAsync(DateTime storeDate, int index = 0)
        {
            var itemStore = await _db.SteamItemStores
                .OrderByDescending(x => x.Start)
                .Where(x => storeDate >= x.Start)
                .FirstOrDefaultAsync();

            if (itemStore != null)
            {
                itemStore.Notes = new PersistableStringCollection(itemStore.Notes);
                itemStore.Notes.Remove(itemStore.Notes.ElementAt(index));

                await _db.SaveChangesAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Fail("Unable to find a store for the specific date");
            }
        }

        [Command("rebuild-store-item-mosaics")]
        public async Task<RuntimeResult> RebuildStoreItemMosaicsAsync()
        {
            var itemStores = await _db.SteamItemStores
                .Where(x => x.ItemsThumbnailId == null)
                .Where(x => x.Items.Any())
                .Include(x => x.Items).ThenInclude(x => x.Item).ThenInclude(x => x.Description)
                .ToArrayAsync();

            var message = await Context.Message.ReplyAsync("Rebuilding store item mosaics...");
            foreach (var itemStore in itemStores)
            {
                await message.ModifyAsync(
                    x => x.Content = $"Rebuilding item mosaic for store {itemStore.Start.ToString("d")} ({Array.IndexOf(itemStores, itemStore) + 1}/{itemStores.Length})..."
                );

                // Generate store thumbnail
                var items = itemStore.Items.Select(x => x.Item).OrderBy(x => x.Description?.Name);
                var itemImageSources = items
                    .Where(x => x.Description != null)
                    .Select(x => new ImageSource()
                    {
                        Title = x.Description.Name,
                        ImageUrl = x.Description.IconUrl,
                        ImageData = x.Description.Icon?.Data,
                    })
                    .ToList();
                if (!itemImageSources.Any())
                {
                    continue;
                }

                var thumbnail = await _queryProcessor.ProcessAsync(new GetImageMosaicRequest()
                {
                    ImageSources = itemImageSources,
                    TileSize = 256,
                    Columns = 3
                });
                if (thumbnail == null)
                {
                    continue;
                }

                itemStore.ItemsThumbnail = new FileData()
                {
                    MimeType = thumbnail.MimeType,
                    Data = thumbnail.Data
                };

                await _db.SaveChangesAsync();
            }

            await message.ModifyAsync(
                x => x.Content = $"Rebuilt {itemStores.Where(x => x.ItemsThumbnailId != null).Count()}/{itemStores.Length} snapshots item mosaics"
            );

            return CommandResult.Success();
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

            // Get all videos by ThatGermanGuy
            var videos = await _googleClient.ListChannelVideosAsync("UCvCBuwbtKRwM0qMi7rc7CUw", maxResults: null);

            // Adjustment for videos that don't follow the naming convention...
            var whitelistedVideoIds = new string[] {
                "AC2OwYiTbio", // Rust Trick or Treat | Halloween 2020 Week 2 Candy Hunter, Little Nightmare, Woodenstein #192
                "dHjMxTY-UAg", // Rust Top Skins | Complete Sets Week, Tactical, Apoc Knight, Loot Leader #146 (Rust Skin Preview)
                "9jjo9TMMpnI" // Rust Top Skins | No Mercy Lr, Metal Beast, Doodle, Sun Praiser, & Looter Sets #74 (Rust Skin Picks)
            };
            var blacklistedVideoIds = new string[] {
                "pbLCjiOZw6s", //Rust Skins | Charitable Rust 2019 Skin Contest, Pencils of Promise Charity 
            };

            // Find store videos following the naming convention
            var rustStoreVideos = videos
                .Where(x => Regex.IsMatch(x.Title, @"^.*Rust.*\|", RegexOptions.IgnoreCase))
                .Where(x => Regex.IsMatch(x.Title, @"^.*Skins.*\|", RegexOptions.IgnoreCase))
                .Where(x =>
                    (
                        x.Title.Contains("Preview", StringComparison.InvariantCultureIgnoreCase) &&
                        !x.Title.Contains("Top Skins", StringComparison.InvariantCultureIgnoreCase)
                    )
                    ||
                    (
                        !Regex.IsMatch(x.Title, @"^.*Top.*\|", RegexOptions.IgnoreCase) &&  // Rust Top Skin | Lunar New Year 2020 Edition #77 (Rust Skin Picks)
                        !Regex.IsMatch(x.Title, @"^.*Picks.*\|", RegexOptions.IgnoreCase) && // Rust Skin Picks | July Week 2 | Cajun & Night Assassin Sets, The Beast & Mimic #2 (Rust Skin Picks)
                        !x.Title.Contains("Coming", StringComparison.InvariantCultureIgnoreCase) && // Rust What's Coming | Multiple Riders in Ch47 & Sedan, Vehicle Progress, New Hair #119 (Rust Updates)
                        !x.Title.Contains("Barrel", StringComparison.InvariantCultureIgnoreCase) && // Rust Skins | Unboxing Weapon Barrels and Box! Halloween Loot #4 (Rust Skins & Unboxings)
                        !x.Title.Contains("Tutorials", StringComparison.InvariantCultureIgnoreCase) && //  Rust | All Red Key Card Monument Puzzles, How to Get Mega Loot (Rust Tutorials)
                        !x.Title.Contains("Twitch Drops", StringComparison.InvariantCultureIgnoreCase) && // Rust Skins | Twitch Drops March 4th 2021 Round 6 (Rust Twitch Drops)
                        !x.Title.Contains("Skin Picks", StringComparison.InvariantCultureIgnoreCase) // Rust Skins | Most Wanted 2020 Top Picks #108 (Skin Picks)
                    )
                )
                .Union(videos.Where(x => whitelistedVideoIds.Contains(x.Id)))
                .Except(videos.Where(x => blacklistedVideoIds.Contains(x.Id)))
                .OrderBy(x => x.PublishedAt)
                .ToList();

            var videoList = new StringBuilder();
            foreach (var video in rustStoreVideos.OrderBy(x => x.PublishedAt))
            {
                var nextVideo = rustStoreVideos.Skip(rustStoreVideos.IndexOf(video) + 1).FirstOrDefault();
                var storeDateText = Regex.Match(video.Title, @"\d{1,2}/\d{1,2}/\d{1,2}").Groups.OfType<Group>().Skip(1).FirstOrDefault()?.Value;
                var storeDateStart = !string.IsNullOrEmpty(storeDateText)
                    ? new DateTimeOffset(DateTime.ParseExact(storeDateText, "MM/dd/yy", CultureInfo.InvariantCulture) + new TimeSpan(17, 0, 0), TimeZoneInfo.Utc.BaseUtcOffset)
                    : video.PublishedAt.Value;
                var storeDateEnd = (nextVideo != null ? nextVideo.PublishedAt.Value : storeDateStart.AddDays(7));
                var store = existingStores
                    .Where(x => storeDateStart.Date == x.Start.Date || x.Media.Serialised.Contains(video.Id))
                    .OrderBy(x => x.Start)
                    .FirstOrDefault();

                // Use the earliest start time available
                //storeDateStart = (store != null && store.Start < storeDateStart ? store.Start : storeDateStart);

                if (store != null)
                {
                    // Update store details
                    if (store.Start != storeDateStart)
                    {
                        //store.Start = storeDateStart;
                    }
                    if (!store.Media.Contains(video.Id))
                    {
                        store.Media.Add(video.Id);
                    }
                }
                else
                {
                    // Create new store
                    var storeName = Regex.Match(video.Title, @"\|\s([^\(]*)").Groups.OfType<Group>().Skip(1).FirstOrDefault()?.Value;
                    _db.SteamItemStores.Add(
                        new SteamItemStore()
                        {
                            App = app,
                            Name = storeName,
                            Start = storeDateStart,
                            End = storeDateEnd,
                            Media = new PersistableStringCollection()
                            {
                                video.Id
                            },
                            IsDraft = true
                        }
                    );
                }
            }

            await _db.SaveChangesAsync();
            return CommandResult.Success();
        }

        // store.steampowered.com/itemstore/252490/
        // store.steampowered.com/itemstore/252490/?filter=Featured
        // store.steampowered.com/itemstore/252490/browse/
        // store.steampowered.com/itemstore/252490/browse/?filter=All
        // store.steampowered.com/itemstore/252490/browse/?filter=Weapon
        // store.steampowered.com/itemstore/252490/browse/?filter=Weapons
        // store.steampowered.com/itemstore/252490/browse/?filter=Armor
        // store.steampowered.com/itemstore/252490/browse/?filter=Clothing
        // store.steampowered.com/itemstore/252490/browse/?filter=Shirts
        // store.steampowered.com/itemstore/252490/browse/?filter=Pants
        // store.steampowered.com/itemstore/252490/browse/?filter=Jackets
        // store.steampowered.com/itemstore/252490/browse/?filter=Hats
        // store.steampowered.com/itemstore/252490/browse/?filter=Masks
        // store.steampowered.com/itemstore/252490/browse/?filter=Footwear
        // store.steampowered.com/itemstore/252490/browse/?filter=Misc
        [Command("import-web-archive-store-prices")]
        public async Task<RuntimeResult> ImportWebArchiveStorePricesAsync(string itemStoreUrl)
        {
            using var client = new HttpClient();
            var webArchiveSnapshotResponse = await client.GetAsync(
                $"https://web.archive.org/cdx/search/cdx?url={Uri.EscapeDataString(itemStoreUrl)}"
            );

            webArchiveSnapshotResponse?.EnsureSuccessStatusCode();

            var webArchiveSnapshotResponseText = await webArchiveSnapshotResponse?.Content?.ReadAsStringAsync();
            if (string.IsNullOrEmpty(webArchiveSnapshotResponseText))
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
                        ItemStoreUrl = $"http://web.archive.org/web/{snapshot}/{Uri.EscapeDataString(itemStoreUrl)}",
                        Timestamp = new DateTimeOffset(
                            DateTime.ParseExact(snapshot, "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                            TimeZoneInfo.Utc.BaseUtcOffset
                        )
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
