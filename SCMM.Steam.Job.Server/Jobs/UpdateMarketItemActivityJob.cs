using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Jobs.Cron;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Steam.Job.Server.Jobs
{
    public class UpdateMarketItemActivityJob : CronJobService
    {
        private readonly ILogger<UpdateMarketItemActivityJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public UpdateMarketItemActivityJob(IConfiguration configuration, ILogger<UpdateMarketItemActivityJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<UpdateMarketItemActivityJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var commnityClient = scope.ServiceProvider.GetService<SteamCommunityWebClient>();
            var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
            var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

            // Delete all market activity older than 24hrs
            var yesterday = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(1));
            var expiredActivity = await db.SteamMarketItemActivity
                .Where(x => x.Timestamp < yesterday)
                .ToListAsync();
            if (expiredActivity.Any())
            {
                foreach (var batch in expiredActivity.Batch(100))
                {
                    db.SteamMarketItemActivity.RemoveRange(batch);
                    db.SaveChanges();
                }
            }

            var assetDescriptions = await db.SteamAssetDescriptions
                .Where(x => x.NameId != null && x.MarketItem != null)
                .Select(x => new
                {
                    Id = x.Id,
                    NameId = x.NameId,
                    MarketItemId = x.MarketItem.Id
                })
                .ToListAsync();

            if (!assetDescriptions.Any())
            {
                return;
            }

            var language = db.SteamLanguages.FirstOrDefault(x => x.Name == Constants.SteamLanguageEnglish);
            if (language == null)
            {
                return;
            }

            var currency = db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
            if (currency == null)
            {
                return;
            }

            var id = Guid.NewGuid();
            _logger.LogInformation($"Updating market item activity information (id: {id}, count: {assetDescriptions.Count()})");
            foreach (var assetDescription in assetDescriptions)
            {
                int progress = assetDescriptions.IndexOf(assetDescription);
                int total = assetDescriptions.Count;
                try
                {
                    var response = await commnityClient.GetMarketItemOrdersActivity(
                        new SteamMarketItemOrdersActivityJsonRequest()
                        {
                            ItemNameId = assetDescription.NameId.ToString(),
                            Language = language.SteamId,
                            CurrencyId = currency.SteamId,
                            NoRender = true
                        }
                    );

                    if (response?.Success != true || response?.Activity?.Any() != true)
                    {
                        continue;
                    }

                    foreach (var activity in response.Activity)
                    {
                        var activityType = SteamMarketItemActivityType.Other;
                        switch (activity.Type)
                        {
                            case "SellOrder": activityType = SteamMarketItemActivityType.CreatedSellOrder; break;
                            case "SellOrderMulti": activityType = SteamMarketItemActivityType.CreatedSellOrder; break;
                            case "SellOrderCancel": activityType = SteamMarketItemActivityType.CancelledSellOrder; break;
                            case "BuyOrder": activityType = SteamMarketItemActivityType.CreatedBuyOrder; break;
                            case "BuyOrderMulti": activityType = SteamMarketItemActivityType.CreatedBuyOrder; break;
                            case "BuyOrderCancel": activityType = SteamMarketItemActivityType.CancelledBuyOrder; break;
                            default: activityType = SteamMarketItemActivityType.Other; break;
                        }
                        var newActivity = new SteamMarketItemActivity()
                        {
                            Timestamp = activity.Time.SteamTimestampToDateTimeOffset(),
                            DescriptionId = assetDescription.Id,
                            ItemId = assetDescription.MarketItemId,
                            CurrencyId = currency.Id,
                            Type = activityType,
                            Price = activity.Price,
                            Quantity = activity.Quantity,
                            SellerName = activity.PersonaSeller,
                            SellerAvatarUrl = activity.AvatarSeller,
                            BuyerName = activity.PersonaBuyer,
                            BuyerAvatarUrl = activity.AvatarBuyer
                        };

                        var activityAlreadyExists = db.SteamMarketItemActivity.Any(x =>
                            x.Timestamp == newActivity.Timestamp && 
                            x.DescriptionId == newActivity.DescriptionId && 
                            x.Type == newActivity.Type && 
                            x.Price == newActivity.Price && 
                            x.Quantity == newActivity.Quantity &&
                            x.SellerName == newActivity.SellerName && 
                            x.BuyerName == newActivity.BuyerName
                        );

                        if (!activityAlreadyExists)
                        {
                            db.SteamMarketItemActivity.Add(newActivity);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to update market item activity for '{assetDescription.NameId}'. {ex.Message}");
                }

                db.SaveChanges();
            }

            _logger.LogInformation($"Updated market item activity information (id: {id})");
        }
    }
}
