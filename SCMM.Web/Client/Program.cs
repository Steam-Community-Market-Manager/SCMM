using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
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
            
            builder.Services.AddHttpClient("SCMM.Web.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
                .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

            // Supply HttpClient instances that include access tokens when making requests to the server project
            builder.Services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("SCMM.Web.ServerAPI"));

            builder.Services.AddApiAuthorization();

            await builder.Build().RunAsync();
        }
    }
}
