using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using SCMM.Web.Client;
using Syncfusion.Blazor;
using Syncfusion.Licensing;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<AppState>();
builder.Services.AddSingleton<LocalStorageService>();
builder.Services.AddSingleton<ExternalNavigationManager>();
builder.Services.AddSingleton<DocumentManager>();

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
    config.SnackbarConfiguration.ClearAfterNavigation = true;
    config.SnackbarConfiguration.PreventDuplicates = true;
});

builder.Services.AddSyncfusionBlazor();

SyncfusionLicenseProvider.RegisterLicense(
    "NDYwMDE3QDMxMzkyZTMxMmUzMFE5Y1BKKzFrd3FzbG5EbHJOZVJSVThMRUhEQnVXdUZjUzVNOWlKTDIwWE09"
);

var app = builder.Build();
await app.RunAsync();
