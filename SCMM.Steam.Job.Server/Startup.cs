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
using SCMM.Google.Client;
using SCMM.Google.Client.Extensions;
using SCMM.Shared.Azure.ServiceBus.Extensions;
using SCMM.Shared.Azure.ServiceBus.Middleware;
using SCMM.Shared.Web.Extensions;
using SCMM.Shared.Web.Middleware;
using SCMM.Steam.API;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Jobs;
using System;
using System.Reflection;

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
                options.EnableSensitiveDataLogging(AppDomain.CurrentDomain.IsDebugBuild());
                options.EnableDetailedErrors(AppDomain.CurrentDomain.IsDebugBuild());
            });

            // Service bus
            services.AddAzureServiceBus(
                Configuration.GetConnectionString("ServiceBusConnection")
            );

            // 3rd party clients
            services.AddSingleton(x => Configuration.GetSteamConfiguration());
            services.AddSingleton(x => Configuration.GetGoogleConfiguration());
            services.AddSingleton<SteamSession>();
            services.AddSingleton<GoogleClient>();
            services.AddScoped<SteamCommunityWebClient>();

            // Auto-mapper
            services.AddAutoMapper(typeof(Startup));

            // Command/query/message handlers
            services.AddCommands(typeof(Startup).Assembly, Assembly.Load("SCMM.Discord.API"), Assembly.Load("SCMM.Steam.API"));
            services.AddQueries(typeof(Startup).Assembly, Assembly.Load("SCMM.Discord.API"), Assembly.Load("SCMM.Steam.API"));
            services.AddMessages(typeof(Startup).Assembly);

            // Services
            services.AddScoped<SteamService>();

            // Jobs
            services.AddHostedService<DeleteExpiredImageDataJob>();
            services.AddHostedService<UpdateCurrencyExchangeRatesJob>();
            services.AddHostedService<RepairMissingAppFiltersJob>();
            services.AddHostedService<UpdateAssetDescriptionsJob>();
            services.AddHostedService<CheckForNewMarketItemsJob>();
            services.AddHostedService<CheckForNewStoreItemsJob>();
            services.AddHostedService<CheckYouTubeForNewStoreVideosJobs>();
            services.AddHostedService<UpdateMarketItemOrdersJob>();
            services.AddHostedService<UpdateMarketItemSalesJob>();
            services.AddHostedService<UpdateCurrentStoreStatisticsJob>();
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

            app.UseAzureServiceBusProcessor();
        }
    }
}
