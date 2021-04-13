using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace SCMM.Discord.Bot.App
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            using (host)
            {
                await host.RunAsync();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddDebug();
                    logging.AddConsole();
                    logging.AddApplicationInsights();
                    logging.AddFilter<ApplicationInsightsLoggerProvider>(String.Empty, LogLevel.Warning);
                })
                .ConfigureServices(services =>
                {
                    services.AddApplicationInsightsTelemetryWorkerService(options =>
                    {
                        //options.InstrumentationKey = services.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                    });
                })
                .ConfigureWebJobs(builder =>
                {
                    builder.AddAzureStorage();
                });
    }
}
