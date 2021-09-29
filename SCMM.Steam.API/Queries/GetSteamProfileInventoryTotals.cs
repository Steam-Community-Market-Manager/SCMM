using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.API.Queries
{
    public class GetSteamProfileInventoryTotalsRequest : IQuery<GetSteamProfileInventoryTotalsResponse>
    {
        public string ProfileId { get; set; }

        public string CurrencyId { get; set; }
    }

    public class GetSteamProfileInventoryTotalsResponse
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

    public class GetSteamProfileInventoryTotals : IQueryHandler<GetSteamProfileInventoryTotalsRequest, GetSteamProfileInventoryTotalsResponse>
    {
        private readonly SteamDbContext _db;
        private readonly IQueryProcessor _queryProcessor;

        public GetSteamProfileInventoryTotals(SteamDbContext db, IQueryProcessor queryProcessor)
        {
            _db = db;
            _queryProcessor = queryProcessor;
        }

        public async Task<GetSteamProfileInventoryTotalsResponse> HandleAsync(GetSteamProfileInventoryTotalsRequest request)
        {
            // Resolve the id
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = request.ProfileId
            });

            // Load the currency
            var currency = await _db.SteamCurrencies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SteamId == request.CurrencyId);

            if (currency == null)
            {
                return null;
            }

            // Load the profile inventory
            var profileInventoryItems = await _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => x.Description != null)
                .Select(x => new
                {
                    Quantity = x.Quantity,
                    AcquiredBy = x.AcquiredBy,
                    BuyPrice = x.BuyPrice,
                    ExchangeRateMultiplier = (x.Currency != null ? x.Currency.ExchangeRateMultiplier : 0),
                    // NOTE: This isn't 100% accurate if the store item price is used. Update this to use StoreItem.Prices with the local currency
                    ItemValue = (x.Description.MarketItem != null ? x.Description.MarketItem.LastSaleValue : (x.Description.StoreItem != null ? x.Description.StoreItem.Price ?? 0 : 0)),
                    ItemValue24hrStable = (x.Description.MarketItem != null ? x.Description.MarketItem.Stable24hrValue : (x.Description.StoreItem != null ? x.Description.StoreItem.Price ?? 0 : 0)),
                    ItemExchangeRateMultiplier = (x.Description.MarketItem != null && x.Description.MarketItem.Currency != null ? x.Description.MarketItem.Currency.ExchangeRateMultiplier : (x.Description.StoreItem != null && x.Description.StoreItem.Currency != null ? x.Description.StoreItem.Currency.ExchangeRateMultiplier : 0))
                })
                .ToListAsync();

            if (!profileInventoryItems.Any())
            {
                return null;
            }

            var profileInventory = new
            {
                ItemCount = profileInventoryItems.Count,
                ItemCountWithBuyPrices = profileInventoryItems.Count(x => x.AcquiredBy != SteamProfileInventoryItemAcquisitionType.Other || x.BuyPrice != null),
                TotalItems = profileInventoryItems
                    .Sum(x => x.Quantity),
                TotalInvested = profileInventoryItems
                    .Where(x => x.BuyPrice != null && x.BuyPrice != 0 && x.ExchangeRateMultiplier != 0)
                    .Sum(x => currency.CalculateExchange((decimal)x.BuyPrice / x.ExchangeRateMultiplier) * x.Quantity),
                TotalInvestmentGains = profileInventoryItems
                    .Where(x => x.BuyPrice != null && x.BuyPrice != 0 && x.ExchangeRateMultiplier != 0 && x.ItemValue != 0 && x.ItemExchangeRateMultiplier != 0)
                    .Sum(x => Math.Max(0, (currency.CalculateExchange(((decimal)x.ItemValue / x.ItemExchangeRateMultiplier) - ((decimal)x.BuyPrice / x.ExchangeRateMultiplier)) * x.Quantity))),
                TotalInvestmentLosses = profileInventoryItems
                    .Where(x => x.BuyPrice != null && x.BuyPrice != 0 && x.ExchangeRateMultiplier != 0 && x.ItemValue != 0 && x.ItemExchangeRateMultiplier != 0)
                    .Sum(x => Math.Min(0, (currency.CalculateExchange(((decimal)x.ItemValue / x.ItemExchangeRateMultiplier) - ((decimal)x.BuyPrice / x.ExchangeRateMultiplier)) * x.Quantity))),
                TotalValue = profileInventoryItems
                    .Where(x => x.ItemValue != 0 && x.ItemExchangeRateMultiplier != 0)
                    .Sum(x => currency.CalculateExchange((decimal)x.ItemValue / x.ItemExchangeRateMultiplier) * x.Quantity),
                TotalValue24hrStable = profileInventoryItems
                    .Where(x => x.ItemValue24hrStable != 0 && x.ItemExchangeRateMultiplier != 0)
                    .Sum(x => currency.CalculateExchange((decimal)x.ItemValue24hrStable / x.ItemExchangeRateMultiplier) * x.Quantity)
            };

            // if more than 50% have buy prices set
            var hasSetupInvestment = (profileInventory.ItemCount > 0 && profileInventory.ItemCountWithBuyPrices > 0)
                ? ((int)Math.Round((((decimal)profileInventory.ItemCountWithBuyPrices / profileInventory.ItemCount) * 100), 0) > 50)
                : false;

            return new GetSteamProfileInventoryTotalsResponse()
            {
                Items = profileInventory.TotalItems,
                Invested = (hasSetupInvestment ? profileInventory.TotalInvested : null),
                InvestmentGains = (hasSetupInvestment ? Math.Abs(profileInventory.TotalInvestmentGains) : null),
                InvestmentLosses = (hasSetupInvestment ? Math.Abs(profileInventory.TotalInvestmentLosses) : null),
                InvestmentNetReturn = (hasSetupInvestment ? (profileInventory.TotalInvestmentGains + profileInventory.TotalInvestmentLosses) : null),
                MarketValue = profileInventory.TotalValue,
                MarketMovementValue = (profileInventory.TotalValue - profileInventory.TotalValue24hrStable),
                MarketMovementTime = new DateTimeOffset(DateTime.UtcNow.Date, TimeZoneInfo.Utc.BaseUtcOffset), // start of today (UTC)
            };
        }
    }
}
