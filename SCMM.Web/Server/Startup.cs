using AspNet.Security.OpenId;
using AspNet.Security.OpenId.Steam;
using AutoMapper;
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
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Middleware;
using SCMM.Web.Server.Services;
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

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
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
                }
            );

            services.AddSingleton<DiscordConfiguration>((s) => Configuration.GetDiscoardConfiguration());
            services.AddSingleton<DiscordClient>();

            services.AddSingleton<GoogleConfiguration>((s) => Configuration.GetGoogleConfiguration());
            services.AddSingleton<GoogleClient>();

            services.AddSingleton<SteamConfiguration>((s) => Configuration.GetSteamConfiguration());
            services.AddSingleton<SteamSession>((s) => new SteamSession(s));

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
                        var securityService = ctx.HttpContext.RequestServices.GetRequiredService<SecurityService>();
                        ctx.Principal.AddIdentity(
                            await securityService.LoginSteamProfileAsync(
                                ctx.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                            )
                        );
                    }
                };
            });

            services.AddDbContext<ScmmDbContext>(
                options =>
                {
                    options.UseSqlServer(Configuration.GetConnectionString("SteamDbConnection"));
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            );

            services.AddScoped<SteamCommunityClient>();

            services.AddScoped<ImageService>();
            services.AddScoped<SecurityService>();
            services.AddScoped<SteamService>();
            services.AddScoped<SteamLanguageService>();
            services.AddScoped<SteamCurrencyService>();

            services.AddHostedService<StartDiscordClientJob>();
            services.AddHostedService<RepopulateCacheJob>();
            services.AddHostedService<RefreshSteamSessionJob>();
            services.AddHostedService<UpdateCurrencyExchangeRatesJob>();
            services.AddHostedService<CheckForMissingAppFiltersJob>();
            services.AddHostedService<CheckForMissingAssetTagsJob>();
            services.AddHostedService<CheckForMissingMarketItemIdsJob>();
            services.AddHostedService<CheckForNewMarketItemsJob>();
            services.AddHostedService<CheckForNewAcceptedWorkshopItemsJob>();
            services.AddHostedService<CheckForNewStoreItemsJob>();
            services.AddHostedService<UpdateAssetWorkshopStatisticsJob>();
            services.AddHostedService<UpdateMarketItemOrdersJob>();
            services.AddHostedService<UpdateMarketItemSalesJob>();
            services.AddHostedService<UpdateStoreSalesStatisticsJob>();
            services.AddHostedService<RecalculateMarketItemSnapshotsJob>();

            services.AddAutoMapper(typeof(Startup));

            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddSwaggerGen(c =>
            {
                try
                {
                    c.IncludeXmlComments("SCMM.Web.Server.xml");
                }
                catch (Exception)
                {
                    // Probably haven't generated XML docs, not a deal breaker...
                }
                c.SwaggerDoc("v1",
                    new OpenApiInfo
                    {
                        Title = "SCMM",
                        Description = "Steam Community Market Manager API",
                        Version = "v1"
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
