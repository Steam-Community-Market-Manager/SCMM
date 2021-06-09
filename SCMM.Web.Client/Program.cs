using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using SCMM.Web.Client.Shared.Navigation;
using SCMM.Web.Client.Shared.Storage;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SCMM.Web.Client
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            // TODO: Use Host.CreateDefaultBuilder
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddSingleton<AppState>();
            builder.Services.AddSingleton<LocalStorageService>();
            builder.Services.AddSingleton<ExternalNavigationManager>();

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

            builder.Services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
                config.SnackbarConfiguration.PreventDuplicates = true;
                config.SnackbarConfiguration.NewestOnTop = true;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.RequireInteraction = true;
            });

            await builder.Build().RunAsync();
        }
    }
}
