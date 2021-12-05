using Azure.Identity;
using CommandQuery.DependencyInjection;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SCMM.Azure.AI;
using SCMM.Azure.AI.Extensions;
using SCMM.Azure.ServiceBus.Extensions;
using SCMM.Azure.ServiceBus.Middleware;
using SCMM.Discord.Bot.Server.Middleware;
using SCMM.Discord.Client;
using SCMM.Discord.Client.Extensions;
using SCMM.Fixer.Client;
using SCMM.Fixer.Client.Extensions;
using SCMM.Google.Client;
using SCMM.Google.Client.Extensions;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models.Json;
using SCMM.Shared.Web.Middleware;
using SCMM.Steam.API;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using System.Reflection;

JsonSerializerOptionsExtensions.SetDefaultOptions();

await WebApplication.CreateBuilder(args)
    .ConfigureLogging()
    .ConfigureAppConfiguration()
    .ConfigureServices()
    .Build()
    .Configure()
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
            builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Warning);
        }
        return builder;
    }

    public static WebApplicationBuilder ConfigureAppConfiguration(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddAzureAppConfiguration(
            options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("AppConfigurationConnection");
                if (!String.IsNullOrEmpty(connectionString))
                {
                    options.Connect(builder.Configuration.GetConnectionString("AppConfigurationConnection"))
                        .ConfigureKeyVault(kv => kv.SetCredential(new DefaultAzureCredential()))
                        .Select(KeyFilter.Any, LabelFilter.Null)
                        .Select(KeyFilter.Any, Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
                        .Select(KeyFilter.Any, Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
                }
            },
            optional: true
        );

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
        builder.Services.AddSingleton(x => builder.Configuration.GetDiscordConfiguration());
        builder.Services.AddSingleton(x => builder.Configuration.GetGoogleConfiguration());
        builder.Services.AddSingleton(x => builder.Configuration.GetSteamConfiguration());
        builder.Services.AddSingleton(x => builder.Configuration.GetFixerConfiguration());
        builder.Services.AddSingleton(x => builder.Configuration.GetAzureAiConfiguration());
        builder.Services.AddSingleton<DiscordClient>();
        builder.Services.AddSingleton<GoogleClient>();
        builder.Services.AddSingleton<SteamSession>();
        builder.Services.AddSingleton<FixerWebClient>();
        builder.Services.AddSingleton<AzureAiClient>();
        builder.Services.AddScoped<SteamWebClient>();
        builder.Services.AddScoped<SteamWebApiClient>();
        builder.Services.AddScoped<SteamCommunityWebClient>();

        // Command/query/message handlers
        builder.Services.AddCommands(Assembly.GetEntryAssembly(), Assembly.Load("SCMM.Discord.API"), Assembly.Load("SCMM.Steam.API"));
        builder.Services.AddQueries(Assembly.GetEntryAssembly(), Assembly.Load("SCMM.Discord.API"), Assembly.Load("SCMM.Steam.API"));
        builder.Services.AddMessages(Assembly.GetEntryAssembly());

        // Services
        builder.Services.AddScoped<SteamService>();

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

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"
            );
            endpoints.MapRazorPages();
        });

        app.UseAzureServiceBusProcessor();

        app.UseDiscordClient();

        return app;
    }
}
