using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.API.Commands
{
    public class CalculateSteamProfileInventoryTotalsRequest : ICommand<CalculateSteamProfileInventoryTotalsResponse>
    {
        public string ProfileId { get; set; }

        public string AppId { get; set; }

        public string CurrencyId { get; set; }
    }

    public class CalculateSteamProfileInventoryTotalsResponse
    {
        public int Items { get; set; }

        public long? Invested { get; set; }

        public long? InvestmentGains { get; set; }

        public long? InvestmentLosses { get; set; }

        public long? InvestmentNetReturn { get; set; }

        public long MarketValue { get; set; }

        public long MarketMovementValue { get; set; }

        public DateTimeOffset MarketMovementTime { get; set; }
    }

    public class CalculateSteamProfileInventoryTotals : ICommandHandler<CalculateSteamProfileInventoryTotalsRequest, CalculateSteamProfileInventoryTotalsResponse>
    {
        private readonly SteamDbContext _db;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IServiceBus _serviceBus;

        public CalculateSteamProfileInventoryTotals(SteamDbContext db, IQueryProcessor queryProcessor, IServiceBus serviceBus)
        {
            _db = db;
            _queryProcessor = queryProcessor;
            _serviceBus = serviceBus;
        }

        public async Task<CalculateSteamProfileInventoryTotalsResponse> HandleAsync(CalculateSteamProfileInventoryTotalsRequest request)
        {
            // Resolve the id
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = request.ProfileId
            });

            // Load the currency
            var currency = await _db.SteamCurrencies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SteamId == request.CurrencyId || x.Name == request.CurrencyId);

            if (currency == null)
            {
                return null;
            }

            // Load the profile inventory
            var dayOpenTimestamp = new DateTimeOffset(DateTime.UtcNow.Date, TimeZoneInfo.Utc.BaseUtcOffset);
            var profileInventoryItems = await _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => String.IsNullOrEmpty(request.AppId) || x.App.SteamId == request.AppId)
                .Where(x => x.Description != null)
                .Select(x => new
                {
                    x.Quantity,
                    x.AcquiredBy,
                    x.BuyPrice,
                    IsMarketableOrTradable = (x.Description.IsMarketable || x.Description.MarketableRestrictionDays > 0 || x.Description.IsTradable || x.Description.TradableRestrictionDays > 0),
                    ExchangeRateMultiplier = x.Currency != null ? x.Currency.ExchangeRateMultiplier : 0,
                    ItemStoreValue = x.Description.StoreItem != null ? (x.Description.StoreItem.Price ?? 0) : 0,
                    ItemStoreValues = x.Description.StoreItem != null ? x.Description.StoreItem.Prices : null,
                    ItemMarketValue = x.Description.MarketItem != null ? x.Description.MarketItem.SellOrderLowestPrice : 0,
                    ItemMarketValue24hrStable = x.Description.MarketItem != null ? x.Description.MarketItem.Stable24hrSellOrderLowestPrice : 0,
                    ItemMarketExchangeRateMultiplier = x.Description.MarketItem != null && x.Description.MarketItem.Currency != null ? x.Description.MarketItem.Currency.ExchangeRateMultiplier : 0
                })
                .ToListAsync();

            var profileInventory = new
            {
                ItemCount = profileInventoryItems.Count,
                ItemCountWithBuyPrices = profileInventoryItems.Count(x => x.AcquiredBy != SteamProfileInventoryItemAcquisitionType.Other || x.BuyPrice != null),
                TotalItems = profileInventoryItems
                    .Sum(x => x.Quantity),
                TotalInvested = profileInventoryItems.Where(x => x.IsMarketableOrTradable)
                    .Where(x => x.BuyPrice != null && x.BuyPrice != 0 && x.ExchangeRateMultiplier != 0)
                    .Sum(x => currency.CalculateExchange((decimal)x.BuyPrice / x.ExchangeRateMultiplier) * x.Quantity),
                TotalInvestmentGains = profileInventoryItems.Where(x => x.IsMarketableOrTradable)
                    .Where(x => x.BuyPrice != null && x.BuyPrice != 0 && x.ExchangeRateMultiplier != 0 && ((x.ItemMarketValue > 0 && x.ItemMarketExchangeRateMultiplier != 0) || x.ItemStoreValues?.ContainsKey(currency.Name) == true))
                    .Sum(x => Math.Max(0, ((x.ItemMarketValue > 0 && x.ItemMarketExchangeRateMultiplier != 0 ? currency.CalculateExchange((decimal)x.ItemMarketValue / x.ItemMarketExchangeRateMultiplier) : x.ItemStoreValues.GetOrDefault(currency.Name)) * x.Quantity) - (currency.CalculateExchange((decimal)x.BuyPrice / x.ExchangeRateMultiplier) * x.Quantity))),
                TotalInvestmentLosses = profileInventoryItems.Where(x => x.IsMarketableOrTradable)
                    .Where(x => x.BuyPrice != null && x.BuyPrice != 0 && x.ExchangeRateMultiplier != 0 && ((x.ItemMarketValue > 0 && x.ItemMarketExchangeRateMultiplier != 0) || x.ItemStoreValues?.ContainsKey(currency.Name) == true))
                    .Sum(x => Math.Min(0, ((x.ItemMarketValue > 0 && x.ItemMarketExchangeRateMultiplier != 0 ? currency.CalculateExchange((decimal)x.ItemMarketValue / x.ItemMarketExchangeRateMultiplier) : x.ItemStoreValues.GetOrDefault(currency.Name)) * x.Quantity) - (currency.CalculateExchange((decimal)x.BuyPrice / x.ExchangeRateMultiplier) * x.Quantity))),
                TotalValue = profileInventoryItems.Where(x => x.IsMarketableOrTradable)
                    .Where(x => (x.ItemMarketValue > 0 && x.ItemMarketExchangeRateMultiplier != 0) || x.ItemStoreValues?.ContainsKey(currency.Name) == true)
                    .Sum(x => (x.ItemMarketValue > 0 && x.ItemMarketExchangeRateMultiplier != 0 ? currency.CalculateExchange((decimal)x.ItemMarketValue / x.ItemMarketExchangeRateMultiplier) : x.ItemStoreValues.GetOrDefault(currency.Name)) * x.Quantity),
                TotalValue24hrStable = profileInventoryItems.Where(x => x.IsMarketableOrTradable)
                    .Where(x => (x.ItemMarketValue24hrStable > 0 && x.ItemMarketExchangeRateMultiplier != 0) || x.ItemStoreValues?.ContainsKey(currency.Name) == true)
                    .Sum(x => (x.ItemMarketValue > 0 && x.ItemMarketExchangeRateMultiplier != 0 ? currency.CalculateExchange((decimal)x.ItemMarketValue24hrStable / x.ItemMarketExchangeRateMultiplier) : x.ItemStoreValues.GetOrDefault(currency.Name)) * x.Quantity)
            };

            // if more than 50% have buy prices set
            var hasSetupInvestment = profileInventory.ItemCount > 0 && profileInventory.ItemCountWithBuyPrices > 0
                ? (int)Math.Round((decimal)profileInventory.ItemCountWithBuyPrices / profileInventory.ItemCount * 100, 0) > 50
                : false;

            // Update profile statistics
            var profile = resolvedId.Profile;
            if (profile != null && !String.IsNullOrEmpty(request.AppId))
            {
                // Update last inventory value snapshot
                var inventoryValue = await _db.SteamProfileInventoryValues
                    .Where(x => x.ProfileId == resolvedId.ProfileId)
                    .Where(x => x.App.SteamId == request.AppId)
                    .FirstOrDefaultAsync();
                if (inventoryValue == null)
                {
                    profile.InventoryValues.Add(
                        inventoryValue = new SteamProfileInventoryValue()
                        {
                            Profile = resolvedId.Profile,
                            App = _db.SteamApps.FirstOrDefault(x => x.SteamId == request.AppId)
                        }
                    );
                }

                // NOTE: Don't use currency conversion, keep it in system currency
                inventoryValue.Items = profileInventoryItems.Sum(x => x.Quantity);
                inventoryValue.MarketValue = profileInventoryItems
                    .Where(x => x.IsMarketableOrTradable)
                    .Sum(x => (x.ItemMarketValue > 0 ? x.ItemMarketValue : x.ItemStoreValue) * x.Quantity);

                // Update "whale" role status
                const int WhaleInventoryItemCount = 5000;
                const int WhaleInventoryMarketValueUsd = 1000000; // $10,000.00
                if ((inventoryValue.Items >= WhaleInventoryItemCount) || (inventoryValue.MarketValue >= WhaleInventoryMarketValueUsd))
                {
                    if (!profile.Roles.Contains(Roles.Whale))
                    {
                        profile.Roles = new PersistableStringCollection(profile.Roles);
                        profile.Roles.Add(Roles.Whale);
                    }
                }
                else
                {
                    if (profile.Roles.Contains(Roles.Whale))
                    {
                        profile.Roles = new PersistableStringCollection(profile.Roles);
                        profile.Roles.Remove(Roles.Whale);
                    }
                }
                /*
                if (profile.Roles.Contains(Roles.Whale))
                {
                    // Re-import whale inventories at least once every 24-hrs
                    await _serviceBus.ScheduleMessageFromNowAsync(TimeSpan.FromHours(25), new ImportProfileInventoryMessage()
                    {
                        ProfileId = profile.SteamId
                    });
                }
                */
                await _db.SaveChangesAsync();
            }

            return new CalculateSteamProfileInventoryTotalsResponse()
            {
                Items = profileInventory.TotalItems,
                Invested = hasSetupInvestment ? profileInventory.TotalInvested : null,
                InvestmentGains = hasSetupInvestment ? Math.Abs(profileInventory.TotalInvestmentGains) : null,
                InvestmentLosses = hasSetupInvestment ? Math.Abs(profileInventory.TotalInvestmentLosses) : null,
                InvestmentNetReturn = hasSetupInvestment ? profileInventory.TotalInvestmentGains + profileInventory.TotalInvestmentLosses : null,
                MarketValue = profileInventory.TotalValue,
                MarketMovementValue = profileInventory.TotalValue - profileInventory.TotalValue24hrStable,
                MarketMovementTime = dayOpenTimestamp
            };
        }
    }
}
