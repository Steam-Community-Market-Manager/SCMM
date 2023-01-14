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
using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Extensions;
using SCMM.Azure.ServiceBus.Middleware;
using SCMM.Market.RustyPot.Client;
using SCMM.Shared.Abstractions.Analytics;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Client;
using SCMM.Shared.Client.Configuration;
using SCMM.Shared.Data.Models.Json;
using SCMM.Shared.Web.Statistics;
using SCMM.Steam.Abstractions;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.SteamCMD;
using StackExchange.Redis;
using System.Net;
using System.Reflection;

Console.WriteLine();
Console.WriteLine(" =============== ");
Console.WriteLine(" | SCMM WORKER | ");
Console.WriteLine(" =============== ");
Console.WriteLine();

JsonSerializerOptionsExtensions.SetDefaultOptions();

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var hostBuilder = new HostBuilder()
    .ConfigureLogging()
    .ConfigureAppConfiguration(environment ?? String.Empty, args)
    .ConfigureServices()
    .UseConsoleLifetime()
    .UseEnvironment(environment);

using (var host = hostBuilder.Build())
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    await host.StartAsync();

    var serviceBusProcessor = new ServiceBusProcessorMiddleware(
        (ctx) => Task.CompletedTask,
        host.Services.GetRequiredService<ILogger<ServiceBusProcessorMiddleware>>(),
        host.Services.GetRequiredService<IServiceScopeFactory>(),
        host.Services.GetRequiredService<Azure.Messaging.ServiceBus.Administration.ServiceBusAdministrationClient>(),
        host.Services.GetRequiredService<Azure.Messaging.ServiceBus.ServiceBusClient>(),
        host.Services.GetRequiredService<MessageHandlerTypeCollection>()
    );
    /*
    var rustyPot = new RustyPotWebClient(
        host.Services.GetRequiredService<ILogger<RustyPotWebClient>>(),
        host.Services.GetRequiredService<IServiceBus>()
    );
    var rustyPotMonitorJob = await rustyPot.MonitorAsync();
    */
    logger.LogInformation("Service bus processor is ready to handle messages.");
    await using (serviceBusProcessor)
    {
        await host.WaitForShutdownAsync();
    }
}

public static class HostExtensions
{
    public static IHostBuilder ConfigureLogging(this IHostBuilder builder)
    {
        return builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddDebug();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Microsoft", level => level >= LogLevel.Warning);
            logging.AddFilter("Microsoft.Hosting.Lifetime", level => level >= LogLevel.Warning);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database", level => level >= LogLevel.Warning);
        });
    }

    public static IHostBuilder ConfigureAppConfiguration(this IHostBuilder builder, string environment, string[] args)
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
                            .Select(KeyFilter.Any, environment)
                            .Select(KeyFilter.Any, "scmm-worker-server");
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
            services.AddSingleton<IWebProxy, RotatingWebProxy>();
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetWebProxyConfiguration().Expand();
            });

            // 3rd party clients
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetSteamConfiguration();
            });
            services.AddSingleton<SteamSession>();
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetAzureAiConfiguration();
            });
            services.AddSingleton<IImageAnalysisService, AzureAiClient>();

            services.AddScoped<SteamWebApiClient>();
            services.AddScoped<SteamCommunityWebClient>();
            services.AddScoped<ProxiedSteamCommunityWebClient>();
            services.AddScoped<AuthenticatedProxiedSteamCommunityWebClient>();
            services.AddScoped<ISteamConsoleClient, SteamCmdProcessWrapper>();

            // Command/query/message handlers
            var contactAssemblies = new[]
            {
                Assembly.GetEntryAssembly(),
                Assembly.Load("SCMM.Shared.API"),
                Assembly.Load("SCMM.Discord.API"),
                Assembly.Load("SCMM.Steam.API")
            };
            services.AddCommands(contactAssemblies);
            services.AddQueries(contactAssemblies);
            services.AddMessages(contactAssemblies);
        });
    }
}

