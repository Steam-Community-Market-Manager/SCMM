﻿using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client.Commands;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store.Types;
using System.Globalization;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("set-store-name")]
        public async Task<RuntimeResult> SetStoreNameAsync(string storeId, [Remainder] string storeName)
        {
            var itemStore = _steamDb.SteamItemStores.ToList()
                .Where(x => (!String.IsNullOrEmpty(x.Name) && x.Name == storeId) || (x.Start != null && x.Start.Value.ToString("MMMM d yyyy") == storeId))
                .OrderByDescending(x => x.Start)
                .FirstOrDefault();

            if (itemStore != null)
            {
                itemStore.Name = storeName;
                await _steamDb.SaveChangesAsync();
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
            var itemStore = _steamDb.SteamItemStores.ToList()
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

                await _steamDb.SaveChangesAsync();
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
            var itemStore = _steamDb.SteamItemStores.ToList()
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

                await _steamDb.SaveChangesAsync();
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
            var itemStore = _steamDb.SteamItemStores.ToList()
                .Where(x => (!String.IsNullOrEmpty(x.Name) && x.Name == storeId) || (x.Start != null && x.Start.Value.ToString("MMMM d yyyy") == storeId))
                .OrderByDescending(x => x.Start)
                .FirstOrDefault();

            if (itemStore != null)
            {
                itemStore.Notes = new PersistableStringCollection(itemStore.Notes)
                {
                    note
                };

                await _steamDb.SaveChangesAsync();
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
            var itemStore = _steamDb.SteamItemStores.ToList()
                .Where(x => (!String.IsNullOrEmpty(x.Name) && x.Name == storeId) || (x.Start != null && x.Start.Value.ToString("MMMM d yyyy") == storeId))
                .OrderByDescending(x => x.Start)
                .FirstOrDefault();

            if (itemStore != null)
            {
                itemStore.Notes = new PersistableStringCollection(itemStore.Notes);
                itemStore.Notes.Remove(itemStore.Notes.ElementAt(index));

                await _steamDb.SaveChangesAsync();
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
            var itemStoreIds = _steamDb.SteamItemStores.ToList()
                .Where(x => String.IsNullOrEmpty(storeId) || (!String.IsNullOrEmpty(x.Name) && x.Name == storeId) || (x.Start != null && x.Start.Value.ToString("MMMM d yyyy") == storeId))
                .OrderByDescending(x => x.Start)
                .Select(x => x.Id)
                .ToArray();

            var itemStores = await _steamDb.SteamItemStores
                .Where(x => itemStoreIds.Contains(x.Id))
                .Where(x => x.Items.Any())
                .Include(x => x.App)
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

                var itemsThumbnailImage = await _queryProcessor.ProcessAsync(new GetImageMosaicRequest()
                {
                    ImageSources = itemImageSources,
                    ImageSize = 128,
                    ImageColumns = 3
                });
                if (itemsThumbnailImage == null)
                {
                    continue;
                }

                itemStore.ItemsThumbnailUrl = (
                    await _commandProcessor.ProcessWithResultAsync(new UploadImageToBlobStorageRequest()
                    {
                        Name = $"{itemStore.App.SteamId}-store-items-thumbnail-{Uri.EscapeDataString(itemStore.Start?.Ticks.ToString() ?? itemStore.Name?.ToLower())}",
                        MimeType = itemsThumbnailImage.MimeType,
                        Data = itemsThumbnailImage.Data,
                        ExpiresOn = null, // never
                        Overwrite = true
                    })
                )?.ImageUrl ?? itemStore.ItemsThumbnailUrl;

                await _steamDb.SaveChangesAsync();
            }

            await message.ModifyAsync(
                x => x.Content = $"Rebuilt {itemStores.Where(x => x.ItemsThumbnailUrl != null).Count()}/{itemStores.Length} snapshots item mosaics"
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

                    await _steamDb.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    await Context.Message.ReplyAsync(
                        $"Error importing snapshot {snapshot}, skipping. {ex.Message}"
                    );
                    continue;
                }
            }

            await _steamDb.SaveChangesAsync();
            await message.ModifyAsync(
                x => x.Content = $"Imported {webArchiveSnapshots.Length}/{webArchiveSnapshots.Length} snapshots"
            );

            return CommandResult.Success();
        }


        [Command("import-item-definition-store-prices")]
        public async Task<RuntimeResult> ImportItemDefinitionStorePricesAsync(ulong appId)
        {
            var assetDescriptions = await _steamDb.SteamAssetDescriptions
                .Where(x => x.App.SteamId == appId.ToString())
                .Where(x => !String.IsNullOrEmpty(x.PriceFormat))
                .Where(x => x.StoreItem != null)
                .Where(x => x.StoreItem.Stores.Count == 1) // ignore items that have been released multiple times as it complicates things when the price is different between releases
                .Include(x => x.StoreItem).ThenInclude(x => x.Stores)
                .ToArrayAsync();

            var usdCurrency = await _steamDb.SteamCurrencies
                .FirstOrDefaultAsync(x => x.Name == Constants.SteamCurrencyUSD);

            foreach (var assetDescription in assetDescriptions)
            {
                var prices = assetDescription.PriceFormat.ParseSteamPrices();
                var usdPrice = prices.SteamPriceAtDateTime(assetDescription.TimeAccepted?.DateTime ?? assetDescription.TimeCreated?.DateTime ?? DateTime.UtcNow, usdCurrency.Name);
                if (usdPrice != null)
                {
                    // If the current store item prices is different to the item definition price format, update it
                    if ((ulong)(assetDescription.StoreItem.Price ?? 0) != usdPrice.Price)
                    {
                        // Update the price for the store item releases
                        foreach (var storeItem in assetDescription.StoreItem.Stores)
                        {
                            if ((storeItem.Price ?? 0) == (assetDescription.StoreItem.Price ?? 0))
                            {
                                storeItem.CurrencyId = usdCurrency.Id;
                                storeItem.Currency = usdCurrency;
                                storeItem.Price = (long)usdPrice.Price;
                                storeItem.Prices = new PersistablePriceDictionary()
                                {
                                    { usdCurrency.Name, (long) usdPrice.Price }
                                };
                            }
                        }

                        // Update the latest price for the store item
                        assetDescription.StoreItem.UpdateLatestPrice();
                    }
                }
            }

            await _steamDb.SaveChangesAsync();

            return CommandResult.Success();
        }

        [Command("rebuild-store-item-missing-prices")]
        public async Task<RuntimeResult> RebuildStoreItemMissingPricesAsync(ulong appId)
        {
            var message = await Context.Message.ReplyAsync("Rebuilding missing store item prices...");
            var currencies = await _steamDb.SteamCurrencies.ToListAsync();
            var usdCurrency = currencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);

            var currenciesAndStoreItems = await _steamDb.SteamCurrencies
                .Select(x => new
                {
                    Currency = x,
                    StoreItemsWithMissingPrices = _steamDb.SteamStoreItemItemStore
                        .Where(y => y.Item.App.SteamId == appId.ToString())
                        .Where(y => y.Price != null)
                        .Where(y => !String.IsNullOrEmpty(y.Prices.Serialised) && !y.Prices.Serialised.Contains(x.Name))
                        .Where(y => y.CurrencyId == usdCurrency.Id)
                        .Include(y => y.Item).ThenInclude(y => y.Stores)
                        .Select(y => new
                        {
                            StoreItemItemStore = y,
                            ExchangeRate = _steamDb.SteamCurrencyExchangeRates
                                .Where(z => z.CurrencyId == x.Name)
                                .Where(z => z.Timestamp < (y.Store.Start != null ? y.Store.Start : y.Item.Description.TimeAccepted))
                                .OrderByDescending(z => z.Timestamp)
                                .FirstOrDefault()
                        })
                        .ToArray()
                })
                .ToListAsync();

            var currenciesWithMissingStorePrices = currenciesAndStoreItems.Where(x => x.StoreItemsWithMissingPrices.Length > 0).ToList();
            foreach (var currencyWithMissingStorePrices in currenciesWithMissingStorePrices)
            {
                await message.ModifyAsync(
                    x => x.Content = $"Rebuilding {currencyWithMissingStorePrices.Currency.Name}, {currencyWithMissingStorePrices.StoreItemsWithMissingPrices.Length} missing store item prices were found"
                );

                foreach (var itemExchange in currencyWithMissingStorePrices.StoreItemsWithMissingPrices)
                {
                    if (itemExchange.ExchangeRate != null)
                    {
                        // Update the price for the store item release
                        itemExchange.StoreItemItemStore.Prices = new Steam.Data.Store.Types.PersistablePriceDictionary(itemExchange.StoreItemItemStore.Prices);
                        itemExchange.StoreItemItemStore.Prices[currencyWithMissingStorePrices.Currency.Name] = EconomyExtensions.SteamPriceRounded(
                            itemExchange.ExchangeRate.ExchangeRateMultiplier.CalculateExchange(itemExchange.StoreItemItemStore.Price.Value)
                        );

                        // Update the latest price for the store item
                        itemExchange.StoreItemItemStore?.Item?.UpdateLatestPrice();
                    }
                    else
                    {
                        // Missing exchange rate?!...
                    }
                }

                await _steamDb.SaveChangesAsync();
            }

            await message.ModifyAsync(
                x => x.Content = $"Rebuilt {currenciesWithMissingStorePrices.Count} currencies with missing store item prices"
            );

            return CommandResult.Success();
        }

        [Command("rebuild-store-item-latest-prices")]
        public async Task<RuntimeResult> RebuildStoreItemLatestPricesAsync(ulong appId)
        {
            var message = await Context.Message.ReplyAsync("Rebuilding latest store item prices...");

            var storeItems = await _steamDb.SteamStoreItemItemStore
                .Where(x => x.Item.App.SteamId == appId.ToString())
                .Include(x => x.Item)
                .Include(x => x.Store)
                .ToListAsync();

            foreach (var storeItem in storeItems)
            {
                storeItem.Item.UpdateLatestPrice();
            }

            await _steamDb.SaveChangesAsync();

            await message.ModifyAsync(
                x => x.Content = $"Rebuilt {storeItems.Count} store item prices"
            );

            return CommandResult.Success();
        }
    }
}
