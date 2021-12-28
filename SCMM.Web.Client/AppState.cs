using SCMM.Shared.Data.Models.Json;
using SCMM.Web.Data.Models.UI.Currency;
using SCMM.Web.Data.Models.UI.Language;
using SCMM.Web.Data.Models.UI.Profile;

public class AppState
{
    public const string HttpHeaderLanguage = "language";
    public const string HttpHeaderCurrency = "currency";
    public const string DefaultLanguage = "English";
    public const string DefaultCurrency = "USD";

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
    }

    // TODO: Store this info as a cookie with a fixed-expiry
    public async Task<bool> ReadFromStorageAsync()
    {
        try
        {
            LanguageId = await Storage.GetAsync<string>(nameof(LanguageId));
            CurrencyId = await Storage.GetAsync<string>(nameof(CurrencyId));
            return (!string.IsNullOrEmpty(LanguageId) && !string.IsNullOrEmpty(CurrencyId));
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
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to save state to storage");
        }
    }

    public Task TryGuessLocalityAsync()
    {
        try
        {
            // Just assign defaults
            LanguageId = DefaultLanguage;
            CurrencyId = DefaultCurrency;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, $"Failed to auto-detect locality, falling back to default");
        }

        return Task.CompletedTask;
    }

    public async Task RefreshAsync(HttpClient http)
    {
        try
        {
            AddHeadersTo(http);
            Profile = await http.GetFromJsonWithDefaultsAsync<MyProfileDTO>(
                $"api/profile"
            );
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

    public void ChangeCurrency(CurrencyDetailedDTO currency)
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

        Changed?.Invoke(this, new EventArgs());
    }
}
