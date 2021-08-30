using Microsoft.Extensions.Logging.ApplicationInsights;
using SCMM.Shared.Web;
using CommandQuery.DependencyInjection;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SCMM.Azure.ServiceBus.Extensions;
using SCMM.Azure.ServiceBus.Middleware;
using SCMM.Google.Client;
using SCMM.Google.Client.Extensions;
using SCMM.Shared.Web.Extensions;
using SCMM.Shared.Web.Middleware;
using SCMM.Steam.API;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Jobs;
using System.Reflection;

await WebApplication.CreateBuilder(args)
    .ConfigureLogging()
    .ConfigureServices()
    .Build()
    .Configure()
    .RunAsync();

public static class WebApplicationExtensions
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();
        builder.Logging.AddConsole();
        builder.Logging.AddHtmlLogger();
        builder.Logging.AddApplicationInsights();
        builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Warning);
        return builder;
    }

    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Logging
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        builder.Services.AddApplicationInsightsTelemetry(options =>
        {
            options.InstrumentationKey = builder.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
        });

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
                    options.NonceCookie.SameSite = SameSiteMode.None;
                    options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.CorrelationCookie.IsEssential = true;
                    options.CorrelationCookie.HttpOnly = false;
                    options.CorrelationCookie.SameSite = SameSiteMode.None;
                    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                },
                configureCookieAuthenticationOptions: options =>
                {
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(1);
                    options.Cookie.IsEssential = true;
                    options.Cookie.HttpOnly = false;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                });

        // Database
        builder.Services.AddDbContext<SteamDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("SteamDbConnection"), sql =>
            {
                //sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                sql.EnableRetryOnFailure();
            });
            options.EnableSensitiveDataLogging(AppDomain.CurrentDomain.IsDebugBuild());
            options.EnableDetailedErrors(AppDomain.CurrentDomain.IsDebugBuild());
        });

        // Service bus
        builder.Services.AddAzureServiceBus(
            builder.Configuration.GetConnectionString("ServiceBusConnection")
        );

        // 3rd party clients
        builder.Services.AddSingleton(x => builder.Configuration.GetSteamConfiguration());
        builder.Services.AddSingleton(x => builder.Configuration.GetGoogleConfiguration());
        builder.Services.AddSingleton<SteamSession>();
        builder.Services.AddSingleton<GoogleClient>();
        builder.Services.AddScoped<SteamCommunityWebClient>();

        // Command/query/message handlers
        builder.Services.AddCommands(Assembly.GetEntryAssembly(), Assembly.Load("SCMM.Discord.API"), Assembly.Load("SCMM.Steam.API"));
        builder.Services.AddQueries(Assembly.GetEntryAssembly(), Assembly.Load("SCMM.Discord.API"), Assembly.Load("SCMM.Steam.API"));
        builder.Services.AddMessages(Assembly.GetEntryAssembly());

        // Services
        builder.Services.AddScoped<SteamService>();

        // Jobs
        builder.Services.AddHostedService<CheckForNewStoreItemsJob>();
        builder.Services.AddHostedService<UpdateMarketItemSalesJob>();
        builder.Services.AddHostedService<UpdateMarketItemOrdersJob>();
        builder.Services.AddHostedService<UpdateMarketItemActivityJob>();
        builder.Services.AddHostedService<DeleteExpiredFileDataJob>();
        builder.Services.AddHostedService<UpdateCurrentStoreStatisticsJob>();
        builder.Services.AddHostedService<UpdateAssetDescriptionsJob>();
        builder.Services.AddHostedService<UpdateCurrencyExchangeRatesJob>();
        builder.Services.AddHostedService<CheckForNewMarketItemsJob>();
        builder.Services.AddHostedService<CheckYouTubeForNewStoreVideosJobs>();

        // Controllers
        builder.Services.AddControllersWithViews(options =>
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.Filters.Add(new AuthorizeFilter(policy));
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

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"
            );
            endpoints.MapRazorPages();
        });

        app.UseAzureServiceBusProcessor();

        return app;
    }
}
