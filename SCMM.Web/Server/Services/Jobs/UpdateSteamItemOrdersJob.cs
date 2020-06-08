using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Requests.Community.Json;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class UpdateSteamItemOrdersJob : CronJobService
    {
        private readonly ILogger<UpdateSteamItemOrdersJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public UpdateSteamItemOrdersJob(IConfiguration configuration, ILogger<UpdateSteamItemOrdersJob> logger, IServiceScopeFactory scopeFactory)
            : base(configuration.GetJobConfiguration<UpdateSteamItemOrdersJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

                var itemSteamIds = db.SteamItems
                    .Where(x => !String.IsNullOrEmpty(x.SteamId))
                    .Select(x => new
                    {
                        Id = x.Id,
                        SteamId = x.SteamId
                    })
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
                    UpdateSteamItemOrders(
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
                }
            }
        }

        public async Task UpdateSteamItemOrders(Guid itemId, Guid currencyId, SteamMarketItemOrdersHistogramJsonRequest request)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

                _logger.LogInformation($"Refreshing steam item orders (id: \"{itemId}\")...");
                var orders = await new SteamClient().GetMarketItemOrdersHistogram(request);
                if (orders?.Success != true)
                {
                    _logger.LogWarning($"Refresh of steam item orders (id: \"{itemId}\") failed");
                    return;
                }

                var item = db.SteamItems.SingleOrDefault(x => x.Id == itemId);
                if (item == null)
                {
                    return;
                }

                var buyOrders = orders.BuyOrderTable.Select(x => new SteamItemOrder()
                {
                    Price = x.Price,
                    Quantity = x.Quantity
                });

                var sellOrders = orders.SellOrderTable.Select(x => new SteamItemOrder()
                {
                    Price = x.Price,
                    Quantity = x.Quantity
                });

                item.CurrencyId = currencyId;
                item.RebuildOrders(buyOrders.ToArray(), sellOrders.ToArray());
                item.LastChecked = DateTimeOffset.Now;
                await db.SaveChangesAsync();
            }
        }
    }
}
