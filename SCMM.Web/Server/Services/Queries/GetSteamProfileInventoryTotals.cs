using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Web.Server.Data;
using SCMM.Web.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Queries
{
    public class GetSteamProfileInventoryTotalsRequest : IQuery<GetSteamProfileInventoryTotalsResponse>
    {
        public string ProfileId { get; set; }

        public string CurrencyId { get; set; }
    }

    public class GetSteamProfileInventoryTotalsResponse
    {
        public int TotalItems { get; set; }

        public long? TotalInvested { get; set; }

        public long TotalMarketValue { get; set; }

        public long TotalMarket24hrMovement { get; set; }

        public long TotalResellValue { get; set; }

        public long TotalResellTax { get; set; }

        public long TotalResellProfit { get; set; }
    }

    public class GetSteamProfileInventoryTotals : IQueryHandler<GetSteamProfileInventoryTotalsRequest, GetSteamProfileInventoryTotalsResponse>
    {
        private readonly ScmmDbContext _db;
        private readonly IQueryProcessor _queryProcessor;

        public GetSteamProfileInventoryTotals(ScmmDbContext db, IQueryProcessor queryProcessor)
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
            var currency = _db.SteamCurrencies
                .AsNoTracking()
                .FirstOrDefault(x => x.SteamId == request.CurrencyId);

            if (currency == null)
            {
                return null;
            }

            // Load the profile inventory
            var profileInventoryItems = _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.ProfileId == resolvedId.Id)
                .Where(x => x.Description != null)
                .Select(x => new
                {
                    Quantity = x.Quantity,
                    BuyPrice = x.BuyPrice,
                    ExchangeRateMultiplier = (x.Currency != null ? x.Currency.ExchangeRateMultiplier : 0),
                    // NOTE: This isn't 100% accurate if the store item price is used. Update this to use StoreItem.Prices with the local currency
                    ItemLast1hrValue = (x.Description.MarketItem != null ? x.Description.MarketItem.Last1hrValue : (x.Description.StoreItem != null ? x.Description.StoreItem.Price : 0)),
                    ItemLast24hrValue = (x.Description.MarketItem != null ? x.Description.MarketItem.Last24hrValue : (x.Description.StoreItem != null ? x.Description.StoreItem.Price : 0)),
                    ItemResellPrice = (x.Description.MarketItem != null ? x.Description.MarketItem.ResellPrice : 0),
                    ItemResellTax = (x.Description.MarketItem != null ? x.Description.MarketItem.ResellTax : 0),
                    ItemExchangeRateMultiplier = (x.Description.MarketItem != null && x.Description.MarketItem.Currency != null ? x.Description.MarketItem.Currency.ExchangeRateMultiplier : (x.Description.StoreItem != null && x.Description.StoreItem.Currency != null ? x.Description.StoreItem.Currency.ExchangeRateMultiplier : 0))
                })
                .ToList();

            if (!profileInventoryItems.Any())
            {
                return null;
            }

            var profileInventory = new
            {
                ItemCount = profileInventoryItems.Count,
                ItemCountWithBuyPrices = profileInventoryItems.Count(x => x.BuyPrice != null),
                TotalItems = profileInventoryItems
                    .Sum(x => x.Quantity),
                TotalInvested = profileInventoryItems
                    .Where(x => x.BuyPrice != null && x.BuyPrice != 0 && x.ExchangeRateMultiplier != 0)
                    .Sum(x => (x.BuyPrice / x.ExchangeRateMultiplier) * x.Quantity),
                TotalValueLast1hr = profileInventoryItems
                    .Where(x => x.ItemLast1hrValue != 0 && x.ItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.ItemLast1hrValue / x.ItemExchangeRateMultiplier) * x.Quantity),
                TotalValueLast24hr = profileInventoryItems
                    .Where(x => x.ItemLast24hrValue != 0 && x.ItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.ItemLast24hrValue / x.ItemExchangeRateMultiplier) * x.Quantity),
                TotalResellValue = profileInventoryItems
                    .Where(x => x.ItemResellPrice != 0 && x.ItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.ItemResellPrice / x.ItemExchangeRateMultiplier) * x.Quantity),
                TotalResellTax = profileInventoryItems
                    .Where(x => x.ItemResellTax != 0 && x.ItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.ItemResellTax / x.ItemExchangeRateMultiplier) * x.Quantity)
            };

            var hasSetupInvestment = ((int)Math.Round((((decimal)profileInventory.ItemCountWithBuyPrices / profileInventory.ItemCount) * 100), 0) > 90); // if more than 90% have buy prices set
            return new GetSteamProfileInventoryTotalsResponse()
            {
                TotalItems = profileInventory.TotalItems,
                TotalInvested = (hasSetupInvestment ? currency.CalculateExchange(profileInventory.TotalInvested ?? 0) : null),
                TotalMarketValue = currency.CalculateExchange(profileInventory.TotalValueLast1hr),
                TotalMarket24hrMovement = currency.CalculateExchange(profileInventory.TotalValueLast1hr - profileInventory.TotalValueLast24hr),
                TotalResellValue = currency.CalculateExchange(profileInventory.TotalResellValue),
                TotalResellTax = currency.CalculateExchange(profileInventory.TotalResellTax),
                TotalResellProfit = (
                    currency.CalculateExchange(profileInventory.TotalResellValue - profileInventory.TotalResellTax) - currency.CalculateExchange(profileInventory.TotalInvested ?? 0)
                ),
            };
        }
    }
}
