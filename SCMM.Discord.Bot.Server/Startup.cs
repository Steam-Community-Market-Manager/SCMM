using AutoMapper;
using CommandQuery.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SCMM.Discord.Bot.Server.Middleware;
using SCMM.Discord.Client;
using SCMM.Discord.Client.Extensions;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;

namespace SCMM.Discord.Bot.Server
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
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.InstrumentationKey = Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
            });

            // Authentication
            //...

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

            services.AddSingleton<SteamConfiguration>((s) => Configuration.GetSteamConfiguration());
            services.AddSingleton<SteamSession>((s) => new SteamSession(s));

            services.AddScoped<SteamCommunityClient>();

            // Auto-mapper
            services.AddAutoMapper(typeof(Startup));

            // Command/query handlers
            services.AddCommands(typeof(Startup).Assembly);
            services.AddQueries(typeof(Startup).Assembly);

            // Views
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDevelopmentExceptionHandler();
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
                endpoints.MapRazorPages();
            });
        }
    }
}
