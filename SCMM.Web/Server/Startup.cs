using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SCMM.Steam.Client;
using SCMM.Steam.Shared;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Middleware;
using Microsoft.OpenApi.Models;
using SCMM.Web.Server.Services.Jobs;
using Microsoft.AspNetCore.Authentication.Cookies;
using AspNet.Security.OpenId.Steam;
using System;
using AspNet.Security.OpenId;
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

            var steamConfiguration = Configuration.GetSteamConfiguration();
            services.AddSingleton<SteamConfiguration>((s) => steamConfiguration);
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
                options.ApplicationKey = steamConfiguration.ApplicationKey;
                options.Events = new OpenIdAuthenticationEvents
                {
                    OnTicketReceived = async (ctx) => {
                        var securityService = ctx.HttpContext.RequestServices.GetRequiredService<SecurityService>();
                        ctx.Principal.AddIdentity(
                            await securityService.LoginSteamProfileAsync(
                                ctx.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                            )
                        );
                    }
                };
            });

            services.AddDbContext<SteamDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("SteamDbConnection")
                )
            );

            services.AddTransient<SteamCommunityClient>();

            services.AddTransient<SecurityService>();
            services.AddTransient<SteamService>();
            services.AddTransient<SteamLanguageService>();
            services.AddTransient<SteamCurrencyService>();

            services.AddHostedService<RepopulateCacheJob>();
            services.AddHostedService<RefreshSteamSessionJob>();
            services.AddHostedService<UpdateCurrencyExchangeRatesJob>();
            services.AddHostedService<CheckForMissingAppFiltersJob>();
            services.AddHostedService<CheckForMissingAssetTagsJob>();
            services.AddHostedService<CheckForMissingMarketItemIdsJob>();
            services.AddHostedService<CheckForNewMarketItemsJob>();
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
                c.IncludeXmlComments("SCMM.Web.Server.xml");
                c.SwaggerDoc("v1", 
                    new OpenApiInfo { 
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
                // Enable Swagger API auto-docs
                app.UseSwagger();
                app.UseSwaggerUI(
                    c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SCMM v1")
                );
            }
            else
            {
                app.UseProductionExceptionHandler();
                // Force HTTPS using HSTS
                app.UseHsts();
            }

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
