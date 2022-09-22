using Azure.Identity;
using CommandQuery.DependencyInjection;
using Microsoft.EntityFrameworkCore;
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
using SCMM.Shared.Data.Models.Json;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.SteamCMD;
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
    .ConfigureAppConfiguration(environment, args)
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
            logging.SetMinimumLevel(LogLevel.Information);
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
                            .Select(KeyFilter.Any, environment);
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
                    options.UseSqlServer(steamDbConnectionString, sql =>
                    {
                        //sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        sql.EnableRetryOnFailure();
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
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                });
            }

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
            services.AddSingleton<SteamSession>();
            services.AddSingleton<AzureAiClient>();
            services.AddScoped<SteamWebApiClient>();
            services.AddScoped<SteamCommunityWebClient>();
            services.AddScoped<SteamCmdWrapper>();

            // Command/query/message handlers
            var contactAssemblies = new[]
            {
                Assembly.GetEntryAssembly(),
                Assembly.Load("SCMM.Shared.API"),
                Assembly.Load("SCMM.Discord.API"),
                Assembly.Load("SCMM.Steam.API"),
                Assembly.Load("SCMM.Worker.Client")
            };
            services.AddCommands(contactAssemblies);
            services.AddQueries(contactAssemblies);
            services.AddMessages(contactAssemblies);
        });
    }
}

