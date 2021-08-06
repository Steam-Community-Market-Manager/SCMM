using CommandQuery.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SCMM.Azure.ServiceBus.Extensions;
using SCMM.Steam.API;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using System;
using System.Reflection;

namespace SCMM.Steam.Functions
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services =>
                {
                    // Database
                    services.AddDbContext<SteamDbContext>(options =>
                    {
                        options.UseSqlServer(Environment.GetEnvironmentVariable("SteamDbConnection"), sql =>
                        {
                            //sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                            sql.EnableRetryOnFailure();
                        });
                    });

                    // 3rd party clients
                    services.AddSingleton((services) =>
                    {
                        var configuration = services.GetService<IConfiguration>();
                        return configuration.GetSteamConfiguration();
                    });
                    services.AddSingleton<SteamSession>();
                    services.AddScoped<SteamCommunityWebClient>();
                    services.AddScoped<SteamWorkshopDownloaderWebClient>();

                    // Command/query/message handlers
                    services.AddCommands(typeof(Program).Assembly, Assembly.Load("SCMM.Steam.API"));
                    services.AddQueries(typeof(Program).Assembly, Assembly.Load("SCMM.Steam.API"));
                    services.AddMessages(typeof(Program).Assembly);

                    // Services
                    services.AddScoped<SteamService>();
                })
                .Build();

            host.Run();
        }
    }
}