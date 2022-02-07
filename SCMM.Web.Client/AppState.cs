using SCMM.Shared.Data.Models.Json;
using SCMM.Steam.Data.Models;
using SCMM.Web.Data.Models.UI.Currency;
using SCMM.Web.Data.Models.UI.Language;
using SCMM.Web.Data.Models.UI.App;
using SCMM.Web.Data.Models.UI.Profile;
using SCMM.Web.Data.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class AppState : INotifyPropertyChanged
{
    public const string RuntimeTypeKey = "runtime";
    public const string LanguageNameKey = "language";
    public const string CurrencyNameKey = "currency";
    public const string AppIdKey = "app";

    public const RuntimeType DefaultRuntime = RuntimeType.WebAssembly;
    public const string DefaultLanguage = Constants.SteamLanguageEnglish;
    public const string DefaultCurrency = Constants.SteamCurrencyUSD;
    public const ulong DefaultAppId = Constants.RustAppId;

    private readonly ILogger<AppState> _logger;
    private readonly ICookieManager _cookieManager;

    public AppState(ILogger<AppState> logger, ICookieManager cookieManager)
    {
        _logger = logger;
        _cookieManager = cookieManager;
    }

    private RuntimeType? _runtime;
    public RuntimeType Runtime
    {
        get
        {
            return _runtime ?? DefaultRuntime;
        }
        set
        {
            if (value != _runtime)
            {
                _runtime = value;
                _cookieManager.Set(RuntimeTypeKey, value.ToString());
                NotifyPropertyChanged();
            }
        }
    }

    private bool _isPrerendering;
    public bool IsPrerendering
    {
        get
        {
            return _isPrerendering;
        }
        set
        {
            if (value != _isPrerendering)
            {
                _isPrerendering = value;
                NotifyPropertyChanged();
            }
        }
    }

    private string _languageId;
    public string LanguageId
    {
        get
        {
            return _languageId ?? DefaultLanguage;
        }
        set
        {
            if (value != _languageId)
            {
                _languageId = value;
                if (!string.IsNullOrEmpty(value))
                {
                    _cookieManager.Set(LanguageNameKey, value);
                }
                else
                {
                    _cookieManager.Remove(LanguageNameKey);
                }

                NotifyPropertyChanged();
            }
        }
    }

    private string _currencyId;
    public string CurrencyId
    {
        get
        {
            return _currencyId ?? DefaultCurrency;
        }
        set
        {
            if (value != _languageId)
            {
                _currencyId = value;
                if (!string.IsNullOrEmpty(value))
                {
                    _cookieManager.Set(CurrencyNameKey, value);
                }
                else
                {
                    _cookieManager.Remove(CurrencyNameKey);
                }

                NotifyPropertyChanged();
            }
        }
    }

    private ulong? _appId;
    public ulong AppId
    {
        get
        {
            return _appId ?? DefaultAppId;
        }
        set
        {
            if (value != _appId)
            {
                _appId = value;
                if (value > 0)
                {
                    _cookieManager.Set(AppIdKey, AppId.ToString());
                }
                else
                {
                    _cookieManager.Remove(AppIdKey);
                }

                NotifyPropertyChanged();
            }
        }
    }

    private MyProfileDTO _profile;
    public MyProfileDTO Profile
    {
        get
        {
            return _profile;
        }
        set
        {
            if (value != _profile)
            {
                _profile = value;
                if (value != null)
                {
                    Language = value?.Language;
                    NotifyPropertyChanged(nameof(Language));
                    Currency = value?.Currency;
                    NotifyPropertyChanged(nameof(Currency));
                    App = value?.App;
                    NotifyPropertyChanged(nameof(App));
                }
                NotifyPropertyChanged();
            }
        }
    }

    public LanguageDetailedDTO Language
    {
        get
        {
            return Profile?.Language;
        }
        set
        {
            if (value?.Name != LanguageId)
            {
                LanguageId = value?.Name;
                NotifyPropertyChanged();
                if (Profile != null)
                {
                    Profile.Language = value;
                }
            }
        }
    }

    public CurrencyDetailedDTO Currency
    {
        get
        {
            return Profile?.Currency;
        }
        set
        {
            if (value?.Name != CurrencyId)
            {
                CurrencyId = value?.Name;
                NotifyPropertyChanged();
                if (Profile != null)
                {
                    Profile.Currency = value;
                }
            }
        }
    }

    public AppDetailedDTO App
    {
        get
        {
            return Profile?.App;
        }
        set
        {
            if (value?.Id != AppId)
            {
                AppId = value?.Id ?? 0;
                NotifyPropertyChanged();
                if (Profile != null)
                {
                    Profile.App = value;
                }
            }
        }
    }

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

    public async Task LoadFromCookiesAsync()
    {
        try
        {
            Runtime = await _cookieManager.GetAsync(RuntimeTypeKey, DefaultRuntime);
            LanguageId = await _cookieManager.GetAsync(LanguageNameKey, DefaultLanguage);
            CurrencyId = await _cookieManager.GetAsync(CurrencyNameKey, DefaultCurrency);
            if (UInt64.TryParse(await _cookieManager.GetAsync(AppIdKey, DefaultAppId.ToString()), out ulong appId))
            {
                AppId = appId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load application state from cookies");
        }
    }

    public async Task LoadFromServerProfileAsync(HttpClient http)
    {
        try
        {
            Profile = await http.GetFromJsonWithDefaultsAsync<MyProfileDTO>(
                $"api/profile"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profile state from server");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
