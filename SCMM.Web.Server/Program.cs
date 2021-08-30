﻿using Microsoft.Extensions.Logging.ApplicationInsights;
using SCMM.Shared.Web;
using AspNet.Security.OpenId;
using CommandQuery;
using CommandQuery.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SCMM.Azure.ServiceBus.Extensions;
using SCMM.Azure.ServiceBus.Middleware;
using SCMM.Shared.Web.Extensions;
using SCMM.Shared.Web.Formatters;
using SCMM.Shared.Web.Middleware;
using SCMM.Steam.API;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using System.Reflection;
using System.Security.Claims;
using SCMM.Web.Server;

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
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/signin";
                options.LogoutPath = "/signout";
                options.AccessDeniedPath = "/";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.Cookie.IsEssential = true;
                options.Cookie.HttpOnly = false;
                options.Cookie.SameSite = SameSiteMode.Strict;
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
            /* TODO: Get this to work alongside Steam OpenID
            .AddMicrosoftIdentityWebApp(
                openIdConnectScheme: OpenIdConnectDefaults.AuthenticationScheme,
                cookieScheme: $"{OpenIdConnectDefaults.AuthenticationScheme}{CookieAuthenticationDefaults.AuthenticationScheme}",
                configurationSection: Configuration.GetSection("AzureAd")
            );
            */

        // Authorization
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.Administrator, AuthorizationPolicies.AdministratorBuilder);
            options.AddPolicy(AuthorizationPolicies.User, AuthorizationPolicies.UserBuilder);
            options.DefaultPolicy = options.GetPolicy(AuthorizationPolicies.User);
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
        builder.Services.AddSingleton<SteamSession>();
        builder.Services.AddScoped<SteamCommunityWebClient>();

        // Auto-mapper
        builder.Services.AddAutoMapper(Assembly.GetEntryAssembly());

        // Command/query/message handlers
        builder.Services.AddCommands(Assembly.GetEntryAssembly(), Assembly.Load("SCMM.Discord.API"), Assembly.Load("SCMM.Steam.API"));
        builder.Services.AddQueries(Assembly.GetEntryAssembly(), Assembly.Load("SCMM.Discord.API"), Assembly.Load("SCMM.Steam.API"));
        builder.Services.AddMessages(Assembly.GetEntryAssembly());

        // Services
        builder.Services.AddScoped<SteamService>();
        builder.Services.AddScoped<LanguageCache>();
        builder.Services.AddScoped<CurrencyCache>();

        // Controllers
        builder.Services
            .AddControllers(options =>
            {
                options.Filters.Add<FormatFilter>();
            })
            .AddJsonOptions(options =>
            {
                // TODO: Figure out how to also enable this on client-side
                //options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            })
            .AddXmlSerializerFormatters()
            .AddXlsxSerializerFormatters()
            .AddCsvSerializerFormatters();

        // Views
        builder.Services.AddRazorPages(options =>
        {
            options.Conventions.AuthorizePage("/admin", AuthorizationPolicies.Administrator);
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
            config.SwaggerDoc("v1",
                new OpenApiInfo
                {
                    Title = "SCMM",
                    Version = "v1",
                    Description = (
                        "Steam Community Market Manager (SCMM) API.<br/>" +
                        "These APIs are provided unrestricted, unthrottled, and free of charge in the hopes that they are useful to somebody. If you abuse them, don't be surprised if you get IP banned."
                    ),
                    Contact = new OpenApiContact()
                    {
                        Name = "More about this project",
                        Url = new Uri($"{builder.Configuration.GetWebsiteUrl()}/about")
                    },
                    TermsOfService = new Uri($"{builder.Configuration.GetWebsiteUrl()}/tos")
                }
            );
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
            // Enable WASM debugging
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
        }

        // Enable Swagger API auto-docs
        app.UseSwagger(config =>
        {
            config.RouteTemplate = "docs/{documentname}/swagger.json";
        });
        app.UseSwaggerUI(config =>
        {
            config.RoutePrefix = "docs";
            config.SwaggerEndpoint("/docs/v1/swagger.json", "SCMM v1");
            config.InjectStylesheet("/css/scmm-swagger-theme.css");
            config.OAuth2RedirectUrl("/signin");
        });

        app.UseHttpsRedirection();
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages()
                .RequireAuthorization(AuthorizationPolicies.Administrator);

            endpoints.MapControllers();
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"
            );

            endpoints.MapFallbackToFile("index.html");
        });

        app.UseAzureServiceBusProcessor();

        return app;
    }
}
