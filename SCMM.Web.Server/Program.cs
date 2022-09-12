using AspNet.Security.OpenId;
using Azure.Identity;
using CommandQuery;
using CommandQuery.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.OpenApi.Models;
using SCMM.Azure.AI;
using SCMM.Azure.AI.Extensions;
using SCMM.Azure.ApplicationInsights.Filters;
using SCMM.Azure.ServiceBus.Extensions;
using SCMM.Azure.ServiceBus.Middleware;
using SCMM.Redis.Client.Extensions;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models.Json;
using SCMM.Shared.Web.Formatters;
using SCMM.Shared.Web.Middleware;
using SCMM.Steam.API;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.SteamCMD;
using SCMM.Web.Client.Shared.Storage;
using SCMM.Web.Server;
using SCMM.Web.Server.Shared.Storage;
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
                options.UseSqlServer(dbConnectionString, sql =>
                {
                    //sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    sql.EnableRetryOnFailure();
                });
                options.EnableSensitiveDataLogging(AppDomain.CurrentDomain.IsDebugBuild());
                options.EnableDetailedErrors(AppDomain.CurrentDomain.IsDebugBuild());
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
            builder.Services.AddRedis(redisConnectionString);
        }

        // 3rd party clients
        builder.Services.AddSingleton(x => builder.Configuration.GetSteamConfiguration());
        builder.Services.AddSingleton(x => builder.Configuration.GetAzureAiConfiguration());
        builder.Services.AddSingleton<SteamSession>();
        builder.Services.AddSingleton<AzureAiClient>();
        builder.Services.AddScoped<SteamWebApiClient>();
        builder.Services.AddScoped<SteamCommunityWebClient>();
        builder.Services.AddScoped<SteamCmdWrapper>();

        // Auto-mapper
        builder.Services.AddAutoMapper(Assembly.GetEntryAssembly());

        // Command/query/message handlers
        var contactAssemblies = new[]
        {
            Assembly.GetEntryAssembly(),
            Assembly.Load("SCMM.Shared.API"),
            Assembly.Load("SCMM.Discord.API"),
            Assembly.Load("SCMM.Steam.API")
        };
        builder.Services.AddCommands(contactAssemblies);
        builder.Services.AddQueries(contactAssemblies);
        builder.Services.AddMessages(contactAssemblies);

        // Services
        builder.Services.AddScoped<SteamService>();
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
                // Probably haven't generated XML docs, not a deal breaker...
            }

            config.SwaggerDoc(
                "preview",
                new OpenApiInfo
                {
                    Title = "SCMM",
                    Version = "Preview",
                    Description = (
                        "Steam Community Market Manager (SCMM) API.<br/>" +
                        "These APIs are provided unrestricted, unthrottled, and free of charge in the hopes that they are useful to somebody. If you abuse them or are the cause of significant performance degradation, don't be surprised if you get blocked."
                    ),
                    Contact = new OpenApiContact()
                    {
                        Name = "About",
                        Url = new Uri($"{builder.Configuration.GetWebsiteUrl()}/about")
                    },
                    TermsOfService = new Uri($"{builder.Configuration.GetWebsiteUrl()}/tos")
                }
            );

            var securitySchema = new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: `Authorization: Bearer {token}`",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            config.AddSecurityDefinition("Bearer", securitySchema);
            config.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securitySchema, new[] { "Bearer" } }
            });

        });

        return builder;
    }

    public static WebApplicationBuilder ConfigureClientServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddUIServices();

        builder.Services.AddScoped<ICookieManager, HttpCookieManager>();
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
                var cks = new List<string>();
                foreach (var cookie in cookies)
                {
                    cks.Add($"{cookie.Key}={cookie.Value}");
                }

                client.DefaultRequestHeaders.Add("Cookie", string.Join(';', cks));
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
            config.SwaggerEndpoint("/docs/preview/swagger.json", "SCMM API (Preview)");
            config.InjectStylesheet("/css/scmm-swagger-theme.css");
            config.OAuth2RedirectUrl("/signin");
        });

        app.UseHttpsRedirection();

        var allowLoopbackConnectHack = app.Environment.IsDevelopment() ? "localhost:* wss://localhost:*" : null;
        app.UseOWASPSecurityHeaders(
            cspScriptSources: "'self' 'unsafe-inline' 'unsafe-eval' cdnjs.cloudflare.com cdn.jsdelivr.net cdn.skypack.dev www.googletagmanager.com www.google-analytics.com",
            cspStyleSources: "'self' 'unsafe-inline' cdnjs.cloudflare.com fonts.googleapis.com www.google-analytics.com",
            cspFontSources: "'self' data: cdnjs.cloudflare.com fonts.gstatic.com",
            cspImageSources: "'self' data: blob: *.scmm.app steamcommunity-a.akamaihd.net steamuserimages-a.akamaihd.net steamcdn-a.akamaihd.net avatars.steamstatic.com avatars.akamai.steamstatic.com cdn.discordapp.com www.google-analytics.com",
            cspFrameSources: "'self' www.youtube.com",
            cspConnectSources: $"'self' *.scmm.app discordapp.com www.google-analytics.com stats.g.doubleclick.net {allowLoopbackConnectHack}",
            cspAllowCrossOriginEmbedding: true
        );

        app.UseBlazorFrameworkFiles(); // Wasm
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapDefaultControllerRoute();

            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });

        app.UseAzureServiceBusProcessor();

        return app;
    }
}
