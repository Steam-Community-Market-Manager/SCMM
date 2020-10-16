using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Blazored.LocalStorage;
using Skclusive.Material.Component;

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

            builder.Services.AddBlazoredLocalStorage();
            builder.Services.TryAddMaterialServices(new MaterialConfigBuilder()
                .WithIsPreRendering(false)
                .WithIsServer(false)
                .Build()
            );

            await builder.Build().RunAsync();
        }
    }
}
