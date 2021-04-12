using AspNet.Security.OpenId;
using AspNet.Security.OpenId.Steam;
using AutoMapper;
using CommandQuery;
using CommandQuery.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SCMM.Discord.Client;
using SCMM.Google.Client;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Store;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Middleware;
using SCMM.Web.Server.Services;
using SCMM.Web.Server.Services.Commands;
using SCMM.Web.Server.Services.Jobs;
using System;
using System.Security.Claims;

namespace SCMM.Web.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Logging
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.InstrumentationKey = Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                options.EnableAppServicesHeartbeatTelemetryModule = false;
                options.EnableAzureInstanceMetadataTelemetryModule = true;
                options.EnableDependencyTrackingTelemetryModule = false;
                options.EnableEventCounterCollectionModule = false;
                options.EnablePerformanceCounterCollectionModule = false;
                options.EnableRequestTrackingTelemetryModule = true;
                options.RequestCollectionOptions.TrackExceptions = true;
            });

            // Authentication
            var authConfiguration = services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = SteamAuthenticationDefaults.AuthenticationScheme;
            });
            authConfiguration.AddCookie(options =>
            {
                options.LoginPath = "/signin";
                options.LogoutPath = "/signout";
                options.AccessDeniedPath = "/";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.Cookie.Name = "scmmLoginSecure";
                options.Cookie.IsEssential = true;
                options.Cookie.HttpOnly = false;
            });
            authConfiguration.AddSteam(options =>
            {
                options.ApplicationKey = Configuration.GetSteamConfiguration().ApplicationKey;
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

            // Database
            services.AddDbContext<SteamDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("SteamDbConnection"), sql =>
                {
                    //sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    sql.EnableRetryOnFailure();
                });
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // 3rd party clients
            services.AddSingleton<SCMM.Discord.Client.DiscordConfiguration>((s) => Configuration.GetDiscordConfiguration());
            services.AddSingleton<DiscordClient>();

            services.AddSingleton<GoogleConfiguration>((s) => Configuration.GetGoogleConfiguration());
            services.AddSingleton<GoogleClient>();

            services.AddSingleton<SteamConfiguration>((s) => Configuration.GetSteamConfiguration());
            services.AddSingleton<SteamSession>((s) => new SteamSession(s));

            services.AddScoped<SteamCommunityClient>();

            // Command/query handlers
            services.AddCommands(typeof(Startup).Assembly);
            services.AddQueries(typeof(Startup).Assembly);

            // Services
            services.AddScoped<SteamService>();
            services.AddScoped<SteamLanguageService>();
            services.AddScoped<SteamCurrencyService>();

            // Jobs
            services.AddHostedService<StartDiscordClientJob>();
            services.AddHostedService<RepopulateCacheJob>();
            services.AddHostedService<RefreshSteamSessionJob>();
            services.AddHostedService<DeleteExpiredImageDataJob>();
            services.AddHostedService<UpdateCurrencyExchangeRatesJob>();
            services.AddHostedService<RepairMissingAppFiltersJob>();
            services.AddHostedService<UpdateAssetDescriptionsJob>();
            services.AddHostedService<CheckForMissingMarketItemIdsJob>();
            services.AddHostedService<CheckForNewMarketItemsJob>();
            services.AddHostedService<CheckForNewStoreItemsJob>();
            services.AddHostedService<CheckYouTubeForNewStoreVideosJobs>();
            services.AddHostedService<UpdateAssetWorkshopFilesJob>();
            services.AddHostedService<UpdateMarketItemOrdersJob>();
            services.AddHostedService<UpdateMarketItemSalesJob>();
            services.AddHostedService<UpdateStoreSalesStatisticsJob>();
            services.AddHostedService<RecalculateMarketItemSnapshotsJob>();

            // Controllers
            services.AddControllersWithViews();

            // Views
            services.AddRazorPages();

            // Auto-mapper
            services.AddAutoMapper(typeof(Startup));

            // Auto-documentation
            services.AddSwaggerGen(config =>
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
                            "Steam Community Market Manager API.<br/>" +
                            "These APIs are provided unrestricted, unthrottled, and free of charge in the hopes that they are useful to somebody. If I find they are being abused, don't be surprised if they disappear."
                        ),
                        Contact = new OpenApiContact()
                        {
                            Name = "More about this project",
                            Url = new Uri($"{Configuration.GetBaseUrl()}/about")
                        },
                        TermsOfService = new Uri("https://steamcommunity.com/dev/apiterms")
                    }
                );
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
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

            // Enable Swagger API auto-docs
            app.UseSwagger(config =>
            {
                config.RouteTemplate = "docs/{documentname}/swagger.json";
            });
            app.UseSwaggerUI(config =>
            {
                config.RoutePrefix = "docs";
                config.SwaggerEndpoint("/docs/v1/swagger.json", "SCMM v1");
                config.InjectStylesheet("/css/swagger-dark-theme.css");
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
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
