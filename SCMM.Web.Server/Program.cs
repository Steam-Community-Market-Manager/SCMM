using AspNet.Security.OpenId;
using Azure.Identity;
using CommandQuery;
using CommandQuery.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.OpenApi.Models;
using SCMM.Azure.AI;
using SCMM.Azure.AI.Extensions;
using SCMM.Azure.ApplicationInsights.Filters;
using SCMM.Azure.ServiceBus.Extensions;
using SCMM.Azure.ServiceBus.Middleware;
using SCMM.Shared.Abstractions.Analytics;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Abstractions.WebProxies;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Client;
using SCMM.Shared.Data.Models.Json;
using SCMM.Shared.Web.Formatters;
using SCMM.Shared.Web.Middleware;
using SCMM.Shared.Web.Statistics;
using SCMM.Steam.Abstractions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.SteamCMD;
using SCMM.Web.Client;
using SCMM.Web.Data.Models.Services;
using SCMM.Web.Server;
using SCMM.Web.Server.Services;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Filters;
using System.Net;
using System.Reflection;
using System.Security.Claims;

JsonSerializerOptionsExtensions.SetDefaultOptions();

await WebApplication.CreateBuilder(args)
    .ConfigureLogging()
    .ConfigureAppConfiguration()
    .ConfigureServices()
    .ConfigureClientServices()
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
        builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/signin";
                options.LogoutPath = "/signout";
                options.AccessDeniedPath = "/";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.Cookie.IsEssential = true;
                options.Cookie.HttpOnly = false;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            })
            .AddSteam(options =>
            {
                options.ApplicationKey = builder.Configuration.GetSteamConfiguration().ApplicationKey;
                options.CorrelationCookie.IsEssential = true;
                options.CorrelationCookie.HttpOnly = false;
                options.CorrelationCookie.SameSite = SameSiteMode.None;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Events = new OpenIdAuthenticationEvents
                {
                    OnTicketReceived = async (ctx) =>
                    {
                        var db = ctx.HttpContext.RequestServices.GetRequiredService<SteamDbContext>();
                        var commandProcessor = ctx.HttpContext.RequestServices.GetRequiredService<ICommandProcessor>();
                        var loggedInProfile = await commandProcessor.ProcessWithResultAsync(new LoginSteamProfileRequest()
                        {
                            Claim = ctx.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                        });

                        ctx.Principal.AddIdentity(loggedInProfile.Identity);
                        await db.SaveChangesAsync();
                    }
                };
            });

        // Authorization
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.Administrator, AuthorizationPolicies.AdministratorBuilder);
            options.AddPolicy(AuthorizationPolicies.User, AuthorizationPolicies.UserBuilder);
            options.DefaultPolicy = options.GetPolicy(AuthorizationPolicies.User);
        });

        // Database
        var dbConnectionString = builder.Configuration.GetConnectionString("SteamDbConnection");
        if (!String.IsNullOrEmpty(dbConnectionString))
        {
            builder.Services.AddDbContext<SteamDbContext>(options =>
            {
                options.EnableSensitiveDataLogging(AppDomain.CurrentDomain.IsDebugBuild());
                options.EnableDetailedErrors(AppDomain.CurrentDomain.IsDebugBuild());
                options.ConfigureWarnings(c => c.Log((RelationalEventId.CommandExecuting, LogLevel.Debug)));
                options.ConfigureWarnings(c => c.Log((RelationalEventId.CommandExecuted, LogLevel.Debug)));
                options.UseSqlServer(dbConnectionString, sql =>
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
        builder.Services.AddSingleton<IWebProxyStatisticsService, WebProxyStatisticsService>();
        builder.Services.AddSingleton<IWebProxyManager, RotatingWebProxy>();
        builder.Services.AddSingleton<IWebProxy, RotatingWebProxy>();

        // 3rd party clients
        builder.Services.AddSingleton(x => builder.Configuration.GetSteamConfiguration());
        builder.Services.AddSingleton<SteamSession>();
        builder.Services.AddSingleton(x => builder.Configuration.GetAzureAiConfiguration());
        builder.Services.AddSingleton<IImageAnalysisService, AzureAiClient>();

        builder.Services.AddScoped<SteamWebApiClient>();
        builder.Services.AddScoped<SteamStoreWebClient>();
        builder.Services.AddScoped<SteamCommunityWebClient>();
        builder.Services.AddScoped<ProxiedSteamStoreWebClient>();
        builder.Services.AddScoped<ProxiedSteamCommunityWebClient>();
        builder.Services.AddScoped<AuthenticatedProxiedSteamStoreWebClient>();
        builder.Services.AddScoped<AuthenticatedProxiedSteamCommunityWebClient>();
        builder.Services.AddScoped<ISteamConsoleClient, SteamCmdProcessWrapper>();

        // Auto-mapper
        builder.Services.AddAutoMapper(Assembly.GetEntryAssembly());

        // Command/query/message handlers
        var contactAssemblies = new[]
        {
            Assembly.GetEntryAssembly(),
            Assembly.Load("SCMM.Steam.API"),
            Assembly.Load("SCMM.Discord.API"),
            Assembly.Load("SCMM.Shared.API"),
            Assembly.Load("SCMM.Shared.Web")
        };
        builder.Services.AddCommands(contactAssemblies);
        builder.Services.AddQueries(contactAssemblies);
        builder.Services.AddMessages(contactAssemblies);

        // Services
        builder.Services.AddScoped<LanguageCache>();
        builder.Services.AddScoped<CurrencyCache>();
        builder.Services.AddScoped<AppCache>();

        // Controllers
        builder.Services
            .AddControllers(options =>
            {
                options.Filters.Add<FormatFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.UseDefaults();
            })
            .AddXmlSerializerFormatters()
            .AddXlsxSerializerFormatters()
            .AddCsvSerializerFormatters();

        // Views
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor(options =>
        {
            options.DetailedErrors = true;
        });

        // Auto-documentation
        builder.Services.AddSwaggerGen(config =>
        {
            try
            {
                config.IncludeXmlComments("SCMM.Web.Server.xml");
            }
            catch (Exception)
            {
                // We probably haven't generated XML docs for this build, not a deal breaker though...
            }

            config.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "SCMM",
                    Version = "v1",
                    Description = (
                        "Steam Community Market Manager (SCMM) API.<br/>" +
                        "These APIs are provided unrestricted, unthrottled, and free of charge in the hopes that they are useful to somebody. If you abuse them or are the cause of significant performance degradation, don't be surprised if you get blocked.<br/>" +
                        "<br/>" +
                        "Contact: support@scmm.app"
                    ),
                    Contact = new OpenApiContact()
                    {
                        Name = "About",
                        Url = new Uri($"{builder.Configuration.GetWebsiteUrl()}/about")
                    },
                    TermsOfService = new Uri($"{builder.Configuration.GetWebsiteUrl()}/tos")
                }
            );

            // NOTE: App is effectively supplied in the subdomain of the URL, don't need to ask the user for it
            //config.OperationFilter<AddHeaderOperationFilter>(
            //    AppState.AppIdKey, "Steam Application Id used for the request (e.g. 252490 = Rust, 730 = CSGO, etc)", false
            //);
            config.OperationFilter<AddHeaderOperationFilter>(
                AppState.LanguageNameKey, "Language used for the request (e.g. English)", false
            );
            config.OperationFilter<AddHeaderOperationFilter>(
                AppState.CurrencyNameKey, "Currency used for the request (e.g. USD)", false
            );

            var securitySchema = new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the bearer scheme.<br /> Example: `Authorization: Bearer {token}`",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
            };

            config.AddSecurityDefinition("OAuth2", securitySchema);

            config.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();
            config.OperationFilter<SecurityRequirementsOperationFilter>(true, "OAuth2");

        });

        builder.Services.AddRequestDecompression();
        builder.Services.AddResponseCompression();
        
        builder.Services.AddOutputCache(options =>
        {
            options.SizeLimit = 256 * 1024 * 1024; // 256MB
            options.MaximumBodySize = 8 * 1024 * 1024; // 8MB
            options.UseCaseSensitivePaths = false;
            options.AddBasePolicy(builder => builder
                .NoCache() // do not cache by default
                .SetVaryByQuery(AppState.LanguageNameKey, AppState.CurrencyNameKey, AppState.AppIdKey)
                .SetVaryByHeader(AppState.LanguageNameKey, AppState.CurrencyNameKey, AppState.AppIdKey)
                .Tag("all")
            );
        });

        return builder;
    }

    public static WebApplicationBuilder ConfigureClientServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddUIServices();

        builder.Services.AddScoped<ICookieManager, HttpContextCookieManager>();
        builder.Services.AddScoped<ISystemService, CommandQuerySystemService>();

        builder.Services.AddScoped<HttpClient>(sp =>
        {
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            var client = new HttpClient()
            {
                BaseAddress = new Uri(navigationManager.BaseUri)
            };

            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var cookies = httpContextAccessor.HttpContext.Request.Cookies;
            if (cookies.Any())
            {
                client.DefaultRequestHeaders.Add("Cookie", 
                    string.Join(';', cookies.Select(x => $"{x.Key}={x.Value}"))
                );

                var languageId = cookies.FirstOrDefault(x => String.Equals(x.Key, AppState.LanguageNameKey, StringComparison.OrdinalIgnoreCase)).Value;
                if (!String.IsNullOrEmpty(languageId))
                {
                    client.DefaultRequestHeaders.Add(AppState.LanguageNameKey, languageId);
                }

                var currencyId = cookies.FirstOrDefault(x => String.Equals(x.Key, AppState.CurrencyNameKey, StringComparison.OrdinalIgnoreCase)).Value;
                if (!String.IsNullOrEmpty(currencyId))
                {
                    client.DefaultRequestHeaders.Add(AppState.CurrencyNameKey, currencyId);
                }

                var appId = cookies.FirstOrDefault(x => String.Equals(x.Key, AppState.AppIdKey, StringComparison.OrdinalIgnoreCase)).Value;
                if (!String.IsNullOrEmpty(appId))
                {
                    client.DefaultRequestHeaders.Add(AppState.AppIdKey, appId);
                }
            }

            return client;
        });

        return builder;
    }

    public static WebApplication Configure(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDevelopmentExceptionHandler();
            // Enable automatic DB migrations
            app.UseMigrationsEndPoint();
            // Enable wasm debugging
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseProductionExceptionHandler();
            // Force HTTPS using HSTS
            app.UseHsts();
        }

        // Prime caches
        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetService<LanguageCache>()?.RepopulateCache();
            scope.ServiceProvider.GetService<CurrencyCache>()?.RepopulateCache();
            scope.ServiceProvider.GetService<AppCache>()?.RepopulateCache();
        }

        // Enable Swagger API auto-docs
        app.UseSwagger(config =>
        {
            config.RouteTemplate = "docs/{documentname}/swagger.json";
        });
        app.UseSwaggerUI(config =>
        {
            config.RoutePrefix = "docs";
            config.SwaggerEndpoint("/docs/v1/swagger.json", "SCMM API (v1)");
            config.InjectStylesheet("/css/scmm-swagger-theme.css");
            config.OAuth2RedirectUrl("/signin");
        });

        app.UseRequestDecompression();

        app.UseHttpsRedirection();

        var allowLoopbackConnectHack = app.Environment.IsDevelopment() ? "localhost:* wss://localhost:*" : null;
        app.UseOWASPSecurityHeaders(
            cspScriptSources: "'self' 'unsafe-inline' 'unsafe-eval' cdnjs.cloudflare.com cdn.jsdelivr.net cdn.skypack.dev www.googletagmanager.com www.google-analytics.com",
            cspStyleSources: "'self' 'unsafe-inline' cdnjs.cloudflare.com fonts.googleapis.com www.google-analytics.com",
            cspFontSources: "'self' data: cdnjs.cloudflare.com fonts.gstatic.com",
            cspImageSources: "'self' data: blob: *.scmm.app *.akamaihd.net *.steamstatic.com cdnjs.cloudflare.com cdn.discordapp.com cdn.smartlydressedgames.com files.facepunch.com www.google-analytics.com",
            cspFrameSources: "'self' www.youtube.com e.widgetbot.io",
            cspConnectSources: $"'self' *.scmm.app steamcommunity.com discordapp.com www.google-analytics.com stats.g.doubleclick.net {allowLoopbackConnectHack}",
            cspAllowCrossOriginEmbedding: true
        );

        app.UseBlazorFrameworkFiles(); // Wasm
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseOutputCache();

        app.MapControllers();
        app.MapDefaultControllerRoute();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.UseAzureServiceBusProcessor();

        return app;
    }
}
