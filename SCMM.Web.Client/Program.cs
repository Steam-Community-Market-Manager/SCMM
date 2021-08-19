using BlazorApplicationInsights;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using SCMM.Web.Client;
using Syncfusion.Blazor;
using Syncfusion.Licensing;

await WebAssemblyHostBuilder.CreateDefault(args)
    .ConfigureComponents()
    .ConfigureServices()
    .Build()
    .RunAsync();

public static class WebAssemblyHostExtensions
{
    public static WebAssemblyHostBuilder ConfigureComponents(this WebAssemblyHostBuilder builder)
    {
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");
        return builder;
    }

    public static WebAssemblyHostBuilder ConfigureServices(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddBlazorApplicationInsights();

        builder.Services.AddSingleton<AppState>();
        builder.Services.AddSingleton<LocalStorageService>();
        builder.Services.AddSingleton<ExternalNavigationManager>();
        builder.Services.AddSingleton<DocumentManager>();
        builder.Services.AddSingleton<UpdateManager>();

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
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
            config.SnackbarConfiguration.MaximumOpacity = 90;
            config.SnackbarConfiguration.NewestOnTop = true;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.RequireInteraction = true;
            config.SnackbarConfiguration.PreventDuplicates = true;
            config.SnackbarConfiguration.ClearAfterNavigation = false;
        });

        builder.Services.AddSyncfusionBlazor();
        SyncfusionLicenseProvider.RegisterLicense(
            "NDg5NDE5QDMxMzkyZTMyMmUzMEdCNGQycm9JUXJDTUVtaFBoMWpSRHY3dGMraUZvRUlUc0REM2NEdlg2K2s9"
        );

        return builder;
    }
}
