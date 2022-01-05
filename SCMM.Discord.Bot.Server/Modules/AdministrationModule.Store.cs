using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client.Commands;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using System.Globalization;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("set-store-name")]
        public async Task<RuntimeResult> SetStoreNameAsync(string storeId, [Remainder] string storeName)
        {
            var itemStore = _db.SteamItemStores.ToList()
                .Where(x => (!String.IsNullOrEmpty(x.Name) && x.Name == storeId) || (x.Start != null && x.Start.Value.ToString("MMMM d yyyy") == storeId))
                .OrderByDescending(x => x.Start)
                .FirstOrDefault();

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
        public async Task<RuntimeResult> AddStoreMediaAsync(string storeId, params string[] media)
        {
            var itemStore = _db.SteamItemStores.ToList()
                .Where(x => (!String.IsNullOrEmpty(x.Name) && x.Name == storeId) || (x.Start != null && x.Start.Value.ToString("MMMM d yyyy") == storeId))
                .OrderByDescending(x => x.Start)
                .FirstOrDefault();

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
        public async Task<RuntimeResult> RemoveStoreMediaAsync(string storeId, params string[] media)
        {
            var itemStore = _db.SteamItemStores.ToList()
                .Where(x => (!String.IsNullOrEmpty(x.Name) && x.Name == storeId) || (x.Start != null && x.Start.Value.ToString("MMMM d yyyy") == storeId))
                .OrderByDescending(x => x.Start)
                .FirstOrDefault();

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
        public async Task<RuntimeResult> AddStoreNoteAsync(string storeId, [Remainder] string note)
        {
            var itemStore = _db.SteamItemStores.ToList()
                .Where(x => (!String.IsNullOrEmpty(x.Name) && x.Name == storeId) || (x.Start != null && x.Start.Value.ToString("MMMM d yyyy") == storeId))
                .OrderByDescending(x => x.Start)
                .FirstOrDefault();

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
        public async Task<RuntimeResult> RemoveStoreNoteAsync(string storeId, int index = 0)
        {
            var itemStore = _db.SteamItemStores.ToList()
                .Where(x => (!String.IsNullOrEmpty(x.Name) && x.Name == storeId) || (x.Start != null && x.Start.Value.ToString("MMMM d yyyy") == storeId))
                .OrderByDescending(x => x.Start)
                .FirstOrDefault();

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
        public async Task<RuntimeResult> RebuildStoreItemMosaicsAsync(string storeId = null)
        {
            var message = await Context.Message.ReplyAsync("Finding stores...");
            var itemStoreIds = _db.SteamItemStores.ToList()
                .Where(x => String.IsNullOrEmpty(storeId) || (!String.IsNullOrEmpty(x.Name) && x.Name == storeId) || (x.Start != null && x.Start.Value.ToString("MMMM d yyyy") == storeId))
                .OrderByDescending(x => x.Start)
                .Select(x => x.Id)
                .ToArray();

            var itemStores = await _db.SteamItemStores
                .Where(x => itemStoreIds.Contains(x.Id))
                .Where(x => x.Items.Any())
                .Include(x => x.ItemsThumbnail)
                .Include(x => x.Items).ThenInclude(x => x.Item).ThenInclude(x => x.Description)
                .ToArrayAsync();

            await message.ModifyAsync(
                x => x.Content = "Rebuilding store item mosaics..."
            );

            foreach (var itemStore in itemStores)
            {
                await message.ModifyAsync(
                    x => x.Content = $"Rebuilding item mosaic for store {itemStore.Start?.ToString("d") ?? itemStore.Name} ({Array.IndexOf(itemStores, itemStore) + 1}/{itemStores.Length})..."
                );

                // Generate store thumbnail
                var items = itemStore.Items.Select(x => x.Item).OrderBy(x => x.Description?.Name);
                var itemImageSources = items
                    .Where(x => x.Description != null)
                    .Select(x => new ImageSource()
                    {
                        ImageUrl = x.Description.IconUrl,
                        ImageData = x.Description.Icon?.Data,
                    })
                    .ToList();
                if (!itemImageSources.Any())
                {
                    continue;
                }

                var itemsMosaic = await _queryProcessor.ProcessAsync(new GetImageMosaicRequest()
                {
                    ImageSources = itemImageSources,
                    ImageSize = 200,
                    ImageColumns = 3
                });
                if (itemsMosaic == null)
                {
                    continue;
                }

                itemStore.ItemsThumbnail = itemStore.ItemsThumbnail ?? new FileData();
                itemStore.ItemsThumbnail.MimeType = itemsMosaic.MimeType;
                itemStore.ItemsThumbnail.Data = itemsMosaic.Data;

                await _db.SaveChangesAsync();
            }

            await message.ModifyAsync(
                x => x.Content = $"Rebuilt {itemStores.Where(x => x.ItemsThumbnail != null).Count()}/{itemStores.Length} snapshots item mosaics"
            );

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
