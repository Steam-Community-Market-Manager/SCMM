using Azure.Identity;
using CommandQuery.DependencyInjection;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SCMM.Azure.AI;
using SCMM.Azure.AI.Extensions;
using SCMM.Azure.ApplicationInsights.Filters;
using SCMM.Azure.ServiceBus.Extensions;
using SCMM.Azure.ServiceBus.Middleware;
using SCMM.Discord.Bot.Server.Middleware;
using SCMM.Discord.Client;
using SCMM.Discord.Client.Extensions;
using SCMM.Discord.Data.Store;
using SCMM.Fixer.Client;
using SCMM.Fixer.Client.Extensions;
using SCMM.Redis.Client.Statistics;
using SCMM.Shared.Abstractions.Analytics;
using SCMM.Shared.Abstractions.Finance;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Abstractions.WebProxies;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models.Json;
using SCMM.Shared.Data.Store.Extensions;
using SCMM.Shared.Web.Client;
using SCMM.Shared.Web.Server.Middleware;
using SCMM.Steam.Abstractions;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.SteamCMD;
using StackExchange.Redis;
using System.Net;
using System.Reflection;

JsonSerializerOptionsExtensions.SetDefaultOptions();

await WebApplication.CreateBuilder(args)
    .ConfigureLogging()
    .ConfigureAppConfiguration()
    .ConfigureServices()
    .Build()
    .Configure()
    .Warmup()
    .RunAsync();

public static class WebApplicationExtensions
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddDebug();
            builder.Logging.AddConsole();
        }
        else
        {
            builder.Logging.AddApplicationInsights();
        }
        return builder;
    }

    public static WebApplicationBuilder ConfigureAppConfiguration(this WebApplicationBuilder builder)
    {
        var appConfigConnectionString = builder.Configuration.GetConnectionString("AppConfigurationConnection");
        if (!String.IsNullOrEmpty(appConfigConnectionString))
        {
            builder.Configuration.AddAzureAppConfiguration(
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

        return builder;
    }

    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Logging
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        builder.Services.AddApplicationInsightsTelemetry();
        builder.Services.AddApplicationInsightsTelemetryProcessor<IgnoreSyntheticRequestsFilter>();
        builder.Services.AddApplicationInsightsTelemetryProcessor<Ignore304NotModifiedResponsesFilter>();

        // Authentication
        builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(
                options =>
                {
                    var config = builder.Configuration.GetSection("AzureAd").Get<MicrosoftIdentityOptions>();
                    options.Instance = config.Instance;
                    options.Domain = config.Domain;
                    options.ClientId = config.ClientId;
                    options.TenantId = config.TenantId;
                    options.CallbackPath = config.CallbackPath;
                    options.NonceCookie.IsEssential = true;
                    options.NonceCookie.HttpOnly = false;
                    options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.CorrelationCookie.IsEssential = true;
                    options.CorrelationCookie.HttpOnly = false;
                    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                },
                configureCookieAuthenticationOptions: options =>
                {
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(1);
                    options.Cookie.IsEssential = true;
                    options.Cookie.HttpOnly = false;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                });

        // Database
        var discordDbConnectionString = builder.Configuration.GetConnectionString("DiscordDbConnection");
        if (!String.IsNullOrEmpty(discordDbConnectionString))
        {
            builder.Services.AddDbContextFactory<DiscordDbContext>(options =>
            {
                options.UseCosmos(discordDbConnectionString, "SCMM", cosmos =>
                {
                    cosmos.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Direct);
                });
                options.EnableSensitiveDataLogging(AppDomain.CurrentDomain.IsDebugBuild());
                options.EnableDetailedErrors(AppDomain.CurrentDomain.IsDebugBuild());
            });
        }
        var steamDbConnectionString = builder.Configuration.GetConnectionString("SteamDbConnection");
        if (!String.IsNullOrEmpty(steamDbConnectionString))
        {
            builder.Services.AddDbContext<SteamDbContext>(options =>
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
        var serviceBusConnectionString = builder.Configuration.GetConnectionString("ServiceBusConnection");
        if (!String.IsNullOrEmpty(serviceBusConnectionString))
        {
            builder.Services.AddAzureServiceBus(serviceBusConnectionString);
        }

        // Redis cache
        var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection");
        if (!String.IsNullOrEmpty(redisConnectionString))
        {
            builder.Services.AddSingleton(x => ConnectionMultiplexer.Connect(redisConnectionString));
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
            });

            builder.Services.AddSingleton<IStatisticsService, RedisStatisticsService>();
        }

        // Web proxies
        builder.Services.AddSingleton<IWebProxyUsageStatisticsService, WebProxyUsageStatisticsService>();
        builder.Services.AddSingleton<IWebProxyManager>(x => x.GetRequiredService<RotatingWebProxy>()); // Forward interface requests to our singleton
        builder.Services.AddSingleton<IWebProxy>(x => x.GetRequiredService<RotatingWebProxy>()); // Forward interface requests to our singleton
        builder.Services.AddSingleton<RotatingWebProxy>(); // Boo Elion! (https://github.com/aspnet/DependencyInjection/issues/360)

        // 3rd party clients
        builder.Services.AddSingleton(x => builder.Configuration.GetDiscordConfiguration());
        builder.Services.AddSingleton<DiscordClient>();
        builder.Services.AddSingleton(x => builder.Configuration.GetSteamConfiguration());
        builder.Services.AddSingleton(x => builder.Configuration.GetFixerConfiguration());
        builder.Services.AddSingleton<ICurrencyExchangeService, FixerWebClient>();
        builder.Services.AddSingleton(x => builder.Configuration.GetAzureAiConfiguration());
        builder.Services.AddSingleton<ITimeSeriesAnalysisService>(x => x.GetRequiredService<AzureAiClient>()); // Forward interface requests to our singleton
        builder.Services.AddSingleton<IImageAnalysisService>(x => x.GetRequiredService<AzureAiClient>()); // Forward interface requests to our singleton
        builder.Services.AddSingleton<AzureAiClient>(); // Boo Elion! (https://github.com/aspnet/DependencyInjection/issues/360)

        builder.Services.AddScoped<SteamWebApiClient>();
        builder.Services.AddScoped<SteamStoreWebClient>();
        builder.Services.AddScoped<SteamCommunityWebClient>();
        builder.Services.AddScoped<ISteamConsoleClient, SteamCmdProcessWrapper>();

        // Command/query/message handlers
        var contactAssemblies = new[]
        {
            Assembly.GetEntryAssembly(),
            Assembly.Load("SCMM.Steam.API"),
            Assembly.Load("SCMM.Discord.API"),
            Assembly.Load("SCMM.Shared.API")
        };
        builder.Services.AddCommands(contactAssemblies);
        builder.Services.AddQueries(contactAssemblies);
        builder.Services.AddMessages(contactAssemblies);

        // Controllers
        builder.Services
            .AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                options.Filters.Add(new AuthorizeFilter(policy));
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.UseDefaults();
            });

        // Views
        builder.Services.AddRazorPages()
             .AddMicrosoftIdentityUI();

        return builder;
    }

    public static WebApplication Configure(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDevelopmentExceptionHandler();
            // Enable automatic DB migrations
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseProductionExceptionHandler();
            // Force HTTPS using HSTS
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}"
        );

        app.MapRazorPages();

        app.UseAzureServiceBusProcessor();

        app.UseDiscordClient();

        return app;
    }

    public static WebApplication Warmup(this WebApplication app)
    {
        app.EnsureDatabaseIsInitialised<DiscordDbContext>();

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
