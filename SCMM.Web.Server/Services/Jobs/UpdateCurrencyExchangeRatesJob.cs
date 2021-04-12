using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class UpdateCurrencyExchangeRatesJob : CronJobService
    {
        private readonly ILogger<UpdateCurrencyExchangeRatesJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public UpdateCurrencyExchangeRatesJob(IConfiguration configuration, ILogger<UpdateCurrencyExchangeRatesJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<UpdateCurrencyExchangeRatesJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var commnityClient = scope.ServiceProvider.GetService<SteamCommunityClient>();
                var currencyService = scope.ServiceProvider.GetRequiredService<SteamCurrencyService>();
                var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

                var mostExpensiveItem = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Description)
                    .OrderByDescending(x => x.BuyNowPrice)
                    .FirstOrDefault();

                if (mostExpensiveItem == null)
                {
                    return;
                }

                var language = db.SteamLanguages.FirstOrDefault(x => x.IsDefault);
                if (language == null)
                {
                    return;
                }

                var currencies = db.SteamCurrencies.ToList();
                if (!currencies.Any())
                {
                    return;
                }

                var systemCurrencyPrice = 0L;
                var systemCurrency = currencies.FirstOrDefault(x => x.IsDefault);
                if (systemCurrency == null)
                {
                    return;
                }

                foreach (var currency in currencies.OrderByDescending(x => x.IsDefault))
                {
                    var response = await commnityClient.GetMarketPriceOverview(new SteamMarketPriceOverviewJsonRequest()
                    {
                        AppId = mostExpensiveItem.App.SteamId,
                        MarketHashName = mostExpensiveItem.Description.Name,
                        Language = language.SteamId,
                        CurrencyId = currency.SteamId,
                        NoRender = true
                    });
                    if (response?.Success != true)
                    {
                        continue;
                    }

                    var localPrice = response.LowestPrice.SteamPriceAsInt();
                    if (currency == systemCurrency)
                    {
                        systemCurrencyPrice = localPrice;
                    }
                    if (localPrice > 0 && systemCurrencyPrice > 0)
                    {
                        currency.ExchangeRateMultiplier = ((decimal)localPrice / systemCurrencyPrice);
                    }

                    // TODO: Find a better way to bypass rate limiting.
                    Thread.Sleep(3000);
                }

                db.SaveChanges();

                await currencyService.RepopulateCache();
            }
        }
    }
}
