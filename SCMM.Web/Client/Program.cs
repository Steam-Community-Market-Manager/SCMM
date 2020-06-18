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

            builder.Services.AddSingleton(
                new HttpClient { 
                    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
                }
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
