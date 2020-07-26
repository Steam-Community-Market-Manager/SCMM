using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Skclusive.Material.Component;
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

            builder.RootComponents.Add<App>("app");

            builder.Services.AddBlazoredLocalStorage();

            builder.Services.AddHttpClient("default", (serviceProvider, client) => {
                client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
                var state = serviceProvider.GetService<AppState>();
                if (state != null)
                {
                    state.SetHeadersFor(client);
                }
            });

            builder.Services.AddSingleton<AppState>();
            builder.Services.AddTransient<HttpClient>(
                sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("default")
            );

            builder.Services.TryAddMaterialServices(new MaterialConfigBuilder()
                .WithIsPreRendering(false)
                .WithIsServer(false)
                .Build()
            );
            
            await builder.Build().RunAsync();
        }
    }
}
