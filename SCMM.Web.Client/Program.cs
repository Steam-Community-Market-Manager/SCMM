using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Skclusive.Core.Component;
using Skclusive.Material.Alert;
using Skclusive.Material.Chip;
using Skclusive.Material.Component;
using Skclusive.Material.Core;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SCMM.Web.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddSingleton<AppState>();

            builder.Services.AddHttpClient("default", (serviceProvider, client) =>
            {
                client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
                var state = serviceProvider.GetService<AppState>();
                if (state != null)
                {
                    state.AddHeadersTo(client);
                }
            });

            builder.Services.AddScoped<HttpClient>(
                sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("default")
            );

            var materialConfig = new MaterialConfigBuilder()
                .WithIsPreRendering(false)
                .WithIsServer(false)
                .WithTheme(Theme.Dark)
                .Build();

            builder.Services.AddBlazoredLocalStorage();
            builder.Services.TryAddAlertServices(materialConfig);
            builder.Services.TryAddChipServices(materialConfig);
            builder.Services.TryAddMaterialServices(materialConfig);

            await builder.Build().RunAsync();
        }
    }
}
