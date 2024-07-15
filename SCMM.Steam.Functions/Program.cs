using Azure.Identity;
using CommandQuery.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SCMM.Azure.AI;
using SCMM.Azure.AI.Extensions;
using SCMM.Azure.ApplicationInsights.Filters;
using SCMM.Azure.ServiceBus.Extensions;
using SCMM.Discord.API.Commands;
using SCMM.Google.Client;
using SCMM.Google.Client.Extensions;
using SCMM.Market.Buff.Client;
using SCMM.Market.Buff.Client.Extensions;
using SCMM.Market.CSDeals.Client;
using SCMM.Market.CSTrade.Client;
using SCMM.Market.DMarket.Client;
using SCMM.Market.DMarket.Client.Extensions;
using SCMM.Market.iTradegg.Client;
using SCMM.Market.LootFarm.Client;
using SCMM.Market.ManncoStore.Client;
using SCMM.Market.RapidSkins.Client;
using SCMM.Market.RustTM.Client;
using SCMM.Market.Rustyloot.Client;
using SCMM.Market.ShadowPay.Client;
using SCMM.Market.ShadowPay.Client.Extensions;
using SCMM.Market.SkinBaron.Client;
using SCMM.Market.Skinport.Client;
using SCMM.Market.SkinSerpent.Client;
using SCMM.Market.SkinsMonkey.Client;
using SCMM.Market.SkinsMonkey.Client.Extensions;
using SCMM.Market.SkinSwap.Client;
using SCMM.Market.SkinSwap.Client.Extensions;
using SCMM.Market.SnipeSkins.Client;
using SCMM.Market.SwapGG.Client;
using SCMM.Market.TradeitGG.Client;
using SCMM.Market.Waxpeer.Client;
using SCMM.Redis.Client.Statistics;
using SCMM.Shared.Abstractions.Analytics;
using SCMM.Shared.Abstractions.Media;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Abstractions.WebProxies;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.API.Messages;
using SCMM.Shared.Data.Models.Json;
using SCMM.Shared.Web.Client;
using SCMM.Steam.Abstractions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.SteamCMD;
using SCMM.Webshare.Client;
using SCMM.Webshare.Client.Extensions;
using StackExchange.Redis;
using System.Net;
using System.Reflection;

JsonSerializerOptionsExtensions.SetGlobalDefaultOptions();

await new HostBuilder()
    .ConfigureLogging()
    .ConfigureAppConfiguration()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices()
    .Build()
    .Warmup()
    .RunAsync();

public static class HostExtensions
{
    public static IHostBuilder ConfigureLogging(this IHostBuilder builder)
    {
        return builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            if (AppDomain.CurrentDomain.IsDebugBuild())
            {
                logging.AddDebug();
                logging.AddConsole();
            }
            else
            {
                logging.AddApplicationInsights();
            }
        });
    }

    public static IHostBuilder ConfigureAppConfiguration(this IHostBuilder builder)
    {
        return builder.ConfigureAppConfiguration(config =>
        {
            var appConfigConnectionString = Environment.GetEnvironmentVariable("AppConfigurationConnection");
            if (!String.IsNullOrEmpty(appConfigConnectionString))
            {
                config.AddAzureAppConfiguration(
                    options =>
                    {
                        options.Connect(appConfigConnectionString)
                            .ConfigureKeyVault(kv => kv.SetCredential(new DefaultAzureCredential()))
                            .Select(KeyFilter.Any, LabelFilter.Null)
                            .Select(KeyFilter.Any, Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
                            .Select(KeyFilter.Any, Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
                    },
                    optional: true
                );
            }
        });
    }

    public static IHostBuilder ConfigureServices(this IHostBuilder builder)
    {
        return builder.ConfigureServices(services =>
        {
            // Logging
            /*services.AddApplicationInsightsTelemetry(options =>
            {
                options.EnableRequestTrackingTelemetryModule = true;
                options.EnableDependencyTrackingTelemetryModule = false;
                options.EnableAppServicesHeartbeatTelemetryModule = false;
                options.EnableHeartbeat = false;
            });*/
            services.AddApplicationInsightsTelemetryProcessor<Ignore304NotModifiedResponsesFilter>();
            services.AddApplicationInsightsTelemetryProcessor<IgnoreSyntheticRequestsFilter>();
            services.AddApplicationInsightsTelemetryProcessor<IgnoreStaticWebFilesFilter>();

            // Database
            var steamDbConnectionString = Environment.GetEnvironmentVariable("SteamDbConnection");
            if (!String.IsNullOrEmpty(steamDbConnectionString))
            {
                services.AddDbContext<SteamDbContext>(options =>
                {
                    options.EnableSensitiveDataLogging(AppDomain.CurrentDomain.IsDebugBuild());
                    options.EnableDetailedErrors(AppDomain.CurrentDomain.IsDebugBuild());
                    options.ConfigureWarnings(c => c.Log((RelationalEventId.CommandExecuting, LogLevel.Debug)));
                    options.ConfigureWarnings(c => c.Log((RelationalEventId.CommandExecuted, LogLevel.Debug)));
                    options.UseSqlServer(steamDbConnectionString, sql =>
                    {
                        sql.EnableRetryOnFailure();
                        sql.CommandTimeout(60);
                    });
                });
            }

            // Service bus
            var serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnection");
            if (!String.IsNullOrEmpty(serviceBusConnectionString))
            {
                services.AddAzureServiceBus(serviceBusConnectionString);
            }

            // Redis cache
            var redisConnectionString = Environment.GetEnvironmentVariable("RedisConnection");
            if (!String.IsNullOrEmpty(redisConnectionString))
            {
                services.AddSingleton(x => ConnectionMultiplexer.Connect(redisConnectionString));
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                });

                services.AddSingleton<IStatisticsService, RedisStatisticsService>();
            }

            // Web proxies
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetWebshareConfiguration();
            });
            services.AddSingleton<IWebProxyManagementService, WebshareWebClient>();
            services.AddSingleton<IWebProxyUsageStatisticsService, WebProxyUsageStatisticsService>();
            services.AddSingleton<IWebProxyManager>(x => x.GetRequiredService<RotatingWebProxy>()); // Forward interface requests to our singleton
            services.AddSingleton<IWebProxy>(x => x.GetRequiredService<RotatingWebProxy>()); // Forward interface requests to our singleton
            services.AddSingleton<RotatingWebProxy>(); // Boo Elion! (https://github.com/aspnet/DependencyInjection/issues/360)

            // 3rd party clients
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetSteamConfiguration();
            });
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetAzureAiConfiguration();
            });
            services.AddSingleton<IImageAnalysisService, AzureAiClient>();
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetGoogleConfiguration();
            });
            services.AddSingleton<IVideoStreamingService, GoogleClient>();
            services.AddSingleton<BuffWebClient>();
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetBuffConfiguration();
            });
            services.AddSingleton<CSDealsWebClient>();
            services.AddSingleton<CSTradeWebClient>();
            services.AddSingleton<DMarketWebClient>();
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetDMarketConfiguration();
            });
            services.AddSingleton<iTradeggWebClient>();
            services.AddSingleton<LootFarmWebClient>();
            services.AddSingleton<ManncoStoreWebClient>();
            services.AddSingleton<RapidSkinsWebClient>();
            services.AddSingleton<RustTMWebClient>();
            services.AddSingleton<RustylootWebClient>();
            services.AddSingleton<ShadowPayWebClient>();
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetShadowPayConfiguration();
            });
            services.AddSingleton<SkinBaronWebClient>();
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetSkinSwapConfiguration();
            });
            services.AddSingleton<SkinSwapWebClient>();
            services.AddSingleton<SkinSerpentWebClient>();
            services.AddSingleton<SkinportWebClient>();
            services.AddSingleton<SkinsMonkeyWebClient>();
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetSkinsMonkeyConfiguration();
            });
            services.AddSingleton<SnipeSkinsWebClient>();
            services.AddSingleton<SwapGGWebClient>();
            services.AddSingleton<TradeitGGWebClient>();
            services.AddSingleton<WaxpeerWebClient>();

            services.AddScoped<SteamWebApiClient>();
            services.AddScoped<SteamStoreWebClient>();
            services.AddScoped<SteamCommunityWebClient>();
            services.AddScoped<ISteamConsoleClient, SteamCmdProcessWrapper>();

            // Command/query/message handlers
            var handlerAssemblies = new[]
            {
                Assembly.GetEntryAssembly(), // Include all handlers in SCMM.Steam.Functions
                Assembly.GetAssembly(typeof(SendMessage)), // Include all handlers in SCMM.Discord.API
                Assembly.GetAssembly(typeof(ImportSteamProfile)), // Include all handlers in SCMM.Steam.API
                Assembly.GetAssembly(typeof(ImportProfileMessage)), // Include all handlers in SCMM.Shared.API
            };
            services.AddCommands(handlerAssemblies);
            services.AddQueries(handlerAssemblies);
            services.AddMessages(handlerAssemblies);
        });
    }

    public static IHost Warmup(this IHost app)
    {
        // Prime caches
        using (var scope = app.Services.CreateScope())
        {
            Task.WaitAll(
                scope.ServiceProvider.GetRequiredService<IWebProxyManager>().RefreshProxiesAsync()
            );
        }

        return app;
    }
}
