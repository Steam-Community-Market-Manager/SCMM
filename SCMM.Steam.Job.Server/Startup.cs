using AutoMapper;
using CommandQuery.DependencyInjection;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SCMM.Discord.Client;
using SCMM.Discord.Client.Extensions;
using SCMM.Google.Client;
using SCMM.Google.Client.Extensions;
using SCMM.Shared.Web.Middleware;
using SCMM.Steam.API;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Jobs;

namespace SCMM.Steam.Job.Server
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
            });

            // Authentication
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"));

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

            // Auto-mapper
            services.AddAutoMapper(typeof(Startup), typeof(SteamAutoMapperProfile));

            // Command/query handlers
            services.AddCommands(typeof(Startup).Assembly, typeof(SteamService).Assembly);
            services.AddQueries(typeof(Startup).Assembly, typeof(SteamService).Assembly);

            // Services
            services.AddScoped<SteamService>();
            services.AddScoped<SteamLanguageService>();
            services.AddScoped<SteamCurrencyService>();

            // Jobs
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
            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                options.Filters.Add(new AuthorizeFilter(policy));
            });

            // Views
            services.AddRazorPages()
                 .AddMicrosoftIdentityUI();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
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
        }
    }
}
