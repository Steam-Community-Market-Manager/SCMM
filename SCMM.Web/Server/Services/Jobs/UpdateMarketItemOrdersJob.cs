using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class UpdateMarketItemOrdersJob : CronJobService
    {
        private readonly ILogger<UpdateMarketItemOrdersJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public UpdateMarketItemOrdersJob(IConfiguration configuration, ILogger<UpdateMarketItemOrdersJob> logger, IServiceScopeFactory scopeFactory)
            : base(configuration.GetJobConfiguration<UpdateMarketItemOrdersJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

                var itemSteamIds = db.SteamMarketItems
                    .Where(x => !String.IsNullOrEmpty(x.SteamId))
                    .OrderBy(x => x.LastCheckedOn)
                    .Select(x => new
                    {
                        Id = x.Id,
                        SteamId = x.SteamId
                    })
                    .Take(100)
                    .ToList();

                if (!itemSteamIds.Any())
                {
                    return;
                }

                var language = db.SteamLanguages.FirstOrDefault(x => x.Id.ToString() == Constants.DefaultLanguageId);
                if (language == null)
                {
                    return;
                }

                var currency = db.SteamCurrencies.FirstOrDefault(x => x.Id.ToString() == Constants.DefaultCurrencyId);
                if (currency == null)
                {
                    return;
                }

                foreach (var item in itemSteamIds)
                {
                    await UpdateSteamItemOrders(
                        db,
                        item.Id,
                        currency.Id,
                        new SteamMarketItemOrdersHistogramJsonRequest()
                        {
                            ItemNameId = item.SteamId,
                            Language = language.SteamId,
                            CurrencyId = currency.SteamId,
                            NoRender = true
                        }
                    );

                    await db.SaveChangesAsync();
                }
            }
        }

        public async Task UpdateSteamItemOrders(SteamDbContext db, Guid itemId, Guid currencyId, SteamMarketItemOrdersHistogramJsonRequest request)
        {
            var orders = await new SteamClient().GetMarketItemOrdersHistogram(request);
            if (orders?.Success != true)
            {
                return;
            }

            var item = db.SteamMarketItems.SingleOrDefault(x => x.Id == itemId);
            if (item == null)
            {
                return;
            }

            item.LastCheckedOn = DateTimeOffset.Now;
            item.CurrencyId = currencyId;
            item.RebuildOrders(
                ParseSteamItemOrdersFromGraph(orders.BuyOrderGraph), 
                ParseSteamItemOrdersFromGraph(orders.SellOrderGraph)
            );
        }

        private SteamMarketItemOrder[] ParseSteamItemOrdersFromGraph(string[][] orderGraph)
        {
            var orders = new List<SteamMarketItemOrder>();
            var totalQuantity = 0;
            for (int i = 0; i < orderGraph.Length; i++)
            {
                var price = Int32.Parse(orderGraph[i][0].Replace(",", "").Replace(".", ""));
                var quantity = (Int32.Parse(orderGraph[i][1].Replace(",", "")) - totalQuantity);
                orders.Add(new SteamMarketItemOrder()
                {
                    Price = price,
                    Quantity = quantity,
                });
                totalQuantity += quantity;
            }

            return orders.ToArray();
        }
    }
}
