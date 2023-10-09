using Ljbc1994.Blazor.IntersectionObserver;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using SCMM.Shared.Data.Models.Json;
using SCMM.Web.Client;
using SCMM.Web.Client.Services;
using SCMM.Web.Client.Shared;
using SCMM.Web.Client.Shared.Navigation;
using SCMM.Web.Data.Models.Services;
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

        builder.Services.AddScoped<ICookieManager, JavascriptCookieManager>();
        builder.Services.AddScoped<ISystemService, HttpSystemService>();

        builder.Services.AddScoped<HttpClient>(sp =>
        {
            var appState = sp.GetService<AppState>();
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            var client = new HttpClient()
            {
                BaseAddress = new Uri(navigationManager.BaseUri)
            };
            if (appState != null)
            {
                client.DefaultRequestHeaders.Add(AppState.LanguageNameKey, appState.LanguageId);
                client.DefaultRequestHeaders.Add(AppState.CurrencyNameKey, appState.CurrencyId);
                client.DefaultRequestHeaders.Add(AppState.AppIdKey, appState.AppId.ToString());
                appState.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(AppState.LanguageId))
                    {
                        client.DefaultRequestHeaders.Remove(AppState.LanguageNameKey);
                        client.DefaultRequestHeaders.Add(AppState.LanguageNameKey, appState.LanguageId);
                    }
                    else if (e.PropertyName == nameof(AppState.CurrencyId))
                    {
                        client.DefaultRequestHeaders.Remove(AppState.CurrencyNameKey);
                        client.DefaultRequestHeaders.Add(AppState.CurrencyNameKey, appState.CurrencyId);
                    }
                    else if (e.PropertyName == nameof(AppState.AppId))
                    {
                        client.DefaultRequestHeaders.Remove(AppState.AppIdKey);
                        client.DefaultRequestHeaders.Add(AppState.AppIdKey, appState.AppId.ToString());
                    }
                };
            }

            return client;
        });

        return builder;
    }

    public static void AddUIServices(this IServiceCollection services)
    {
        services.AddScoped<AppState>();

        services.AddScoped<ExternalNavigationManager>();
        services.AddScoped<DocumentManager>();

        services.AddIntersectionObserver();

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
            // FYI: This is a [free] community license key, it isn't a secret.
            //      Sign up for account at https://www.syncfusion.com/, request a community license
            "Mjc1MDgyM0AzMjMzMmUzMDJlMzBTbXdzQ24yR21ua0NlT1JVVEdIeTFBK09YZndGb1l2TEJrRHppYUVXOEtrPQ==" // v23.1.39
        );
    }
}
