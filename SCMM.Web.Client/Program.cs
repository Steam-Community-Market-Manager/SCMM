using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using SCMM.Shared.Data.Models.Json;
using SCMM.Web.Client;
using SCMM.Web.Client.Shared;
using SCMM.Web.Client.Shared.Navigation;
using SCMM.Web.Client.Shared.Storage;
using Syncfusion.Blazor;
using Syncfusion.Licensing;

JsonSerializerOptionsExtensions.SetDefaultOptions();

await WebAssemblyHostBuilder.CreateDefault(args)
    .ConfigureServices()
    .Build()
    .RunAsync();

public static class WebAssemblyHostExtensions
{
    public static WebAssemblyHostBuilder ConfigureServices(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddUIServices();
        builder.Services.AddScoped<HttpClient>(sp =>
        {
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            var client = new HttpClient()
            {
                BaseAddress = new Uri(navigationManager.BaseUri)
            };

            return client;
        });

        return builder;
    }

    public static void AddUIServices(this IServiceCollection services)
    {
        services.AddScoped<AppState>();
        services.AddScoped<ICookieManager, CookieManager>();
        services.AddScoped<ExternalNavigationManager>();
        services.AddScoped<DocumentManager>();

        services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
            config.SnackbarConfiguration.NewestOnTop = true;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.RequireInteraction = true;
            config.SnackbarConfiguration.PreventDuplicates = true;
            config.SnackbarConfiguration.ClearAfterNavigation = false;
        });

        services.AddSyncfusionBlazor();
        SyncfusionLicenseProvider.RegisterLicense(
            "OTQxNTgwQDMyMzAyZTM0MmUzMFF6SjRvTWRLcjhiWUROT0RLbVpKT0s1VENXQUIzaUcvNE1sQXBEZVJzaWc9"
        );
    }
}
