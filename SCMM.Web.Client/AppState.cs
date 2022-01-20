using SCMM.Shared.Data.Models.Json;
using SCMM.Steam.Data.Models;
using SCMM.Web.Data.Models.UI.Currency;
using SCMM.Web.Data.Models.UI.Language;
using SCMM.Web.Data.Models.UI.App;
using SCMM.Web.Data.Models.UI.Profile;

public class AppState
{
    public const string HttpHeaderLanguage = "language";
    public const string HttpHeaderCurrency = "currency";
    public const string HttpHeaderAppId = "appId";

    public const string DefaultLanguage = Constants.SteamLanguageEnglish;
    public const string DefaultCurrency = Constants.SteamCurrencyUSD;
    public const ulong DefaultAppId = Constants.RustAppId;

    private readonly ILogger<AppState> Logger;
    private readonly LocalStorageService Storage;

    public AppState(ILogger<AppState> logger, LocalStorageService storage)
    {
        Logger = logger;
        Storage = storage;
    }

    public event EventHandler Changed;

    public string LanguageId { get; set; }

    public LanguageDetailedDTO Language => Profile?.Language;

    public string CurrencyId { get; set; }

    public CurrencyDetailedDTO Currency => Profile?.Currency;

    public ulong AppId { get; set; }

    public AppDetailedDTO App => Profile?.App;

    public MyProfileDTO Profile { get; set; }

    public bool IsAuthenticated => (
        Profile != null && Profile.Guid != Guid.Empty
    );

    public bool IsInRole(string role)
    {
        if (String.IsNullOrEmpty(role))
        {
            return false;
        }

        return Profile?.Roles?.Any(x => string.Equals(x, role, StringComparison.OrdinalIgnoreCase)) == true;
    }

    public bool Is(string steamId)
    {
        if (String.IsNullOrEmpty(steamId))
        {
            return false;
        }

        return (Profile?.SteamId == steamId || Profile?.ProfileId == steamId) == true;
    }

    public void AddHeadersTo(HttpClient client)
    {
        if (!string.IsNullOrEmpty(LanguageId))
        {
            client.DefaultRequestHeaders.Remove(HttpHeaderLanguage);
            client.DefaultRequestHeaders.Add(HttpHeaderLanguage, LanguageId);
        }
        if (!string.IsNullOrEmpty(CurrencyId))
        {
            client.DefaultRequestHeaders.Remove(HttpHeaderCurrency);
            client.DefaultRequestHeaders.Add(HttpHeaderCurrency, CurrencyId);
        }
        if (AppId > 0)
        {
            client.DefaultRequestHeaders.Remove(HttpHeaderAppId);
            client.DefaultRequestHeaders.Add(HttpHeaderAppId, AppId.ToString());
        }
    }

    // TODO: Store this info as a cookie with a fixed-expiry
    public async Task<bool> ReadFromStorageAsync()
    {
        try
        {
            LanguageId = await Storage.GetAsync<string>(nameof(LanguageId));
            CurrencyId = await Storage.GetAsync<string>(nameof(CurrencyId));
            UInt64.TryParse(await Storage.GetAsync<string>(nameof(AppId)), out ulong appId);
            AppId = appId;
            return (!string.IsNullOrEmpty(LanguageId) && !string.IsNullOrEmpty(CurrencyId) && AppId > 0);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load state from storage");
            return false;
        }
    }

    // TODO: Store this info as a cookie with a fixed-expiry
    public async Task WriteToStorageAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(LanguageId))
            {
                await Storage.SetAsync<string>(nameof(LanguageId), LanguageId);
            }
            else
            {
                await Storage.RemoveAsync(nameof(LanguageId));
            }
            if (!string.IsNullOrEmpty(CurrencyId))
            {
                await Storage.SetAsync<string>(nameof(CurrencyId), CurrencyId);
            }
            else
            {
                await Storage.RemoveAsync(nameof(CurrencyId));
            }
            if (AppId > 0)
            {
                await Storage.SetAsync<string>(nameof(AppId), AppId.ToString());
            }
            else
            {
                await Storage.RemoveAsync(nameof(AppId));
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to save state to storage");
        }
    }

    public async Task RefreshAsync(HttpClient http)
    {
        try
        {
            AddHeadersTo(http);
            Profile = await http.GetFromJsonWithDefaultsAsync<MyProfileDTO>(
                $"api/profile"
            );
            if (Profile != null)
            {
                LanguageId = Profile.Language.Name;
                CurrencyId = Profile.Currency.Name;
                AppId = Profile.App.Id;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh the profile state");
            Profile = null;
        }
        finally
        {
            Changed?.Invoke(this, new EventArgs());
        }
    }

    public async Task ChangeCurrencyAsync(LanguageDetailedDTO language)
    {
        if (language == null)
        {
            return;
        }

        LanguageId = language.Name;
        if (Profile != null)
        {
            Profile.Language = language;
        }

        await WriteToStorageAsync();
        Changed?.Invoke(this, new EventArgs());
    }

    public async Task ChangeCurrencyAsync(CurrencyDetailedDTO currency)
    {
        if (currency == null)
        {
            return;
        }

        CurrencyId = currency.Name;
        if (Profile != null)
        {
            Profile.Currency = currency;
        }

        await WriteToStorageAsync();
        Changed?.Invoke(this, new EventArgs());
    }

    public async Task ChangeAppAsync(AppDetailedDTO app)
    {
        if (app == null)
        {
            return;
        }

        AppId = app.Id;
        if (Profile != null)
        {
            Profile.App = app;
        }

        await WriteToStorageAsync();
        Changed?.Invoke(this, new EventArgs());
    }
}
