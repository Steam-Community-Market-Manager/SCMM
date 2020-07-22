using Blazored.LocalStorage;
using SCMM.Web.Shared;
using SCMM.Web.Shared.Domain.DTOs;
using SCMM.Web.Shared.Domain.DTOs.Currencies;
using SCMM.Web.Shared.Domain.DTOs.Profiles;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SCMM.Web.Client
{
    public class AppState
    {
        private ILocalStorageService _storage;
        private HttpClient _http;

        public AppState(ILocalStorageService storage, HttpClient http)
        {
            _storage = storage;
            _http = http;
        }

        public string LanguageId { get; set; }

        public LanguageStateDTO Language { get; set; }

        public string CurrencyId { get; set; }

        public CurrencyStateDTO Currency { get; set; }

        public string ProfileId { get; set; }

        public ProfileStateDTO Profile { get; set; }

        public event EventHandler Changed;

        public bool IsValid => (
            !String.IsNullOrEmpty(LanguageId) && !String.IsNullOrEmpty(CurrencyId)
        );

        public async Task LoadAsync()
        {
            try
            {
                ProfileId = await _storage.GetItemAsync<string>(nameof(ProfileId));
                LanguageId = await _storage.GetItemAsync<string>(nameof(LanguageId));
                CurrencyId = await _storage.GetItemAsync<string>(nameof(CurrencyId));
                Changed?.Invoke(this, new EventArgs());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load state. {ex.Message}");
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                await _storage.SetItemAsync<string>(nameof(ProfileId), ProfileId);
                await _storage.SetItemAsync<string>(nameof(LanguageId), LanguageId);
                await _storage.SetItemAsync<string>(nameof(CurrencyId), CurrencyId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save state. {ex.Message}");
            }
        }

        public async Task RefreshAsync()
        {
            try
            {
                // GET 
                Changed?.Invoke(this, new EventArgs());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to refresh state. {ex.Message}");
            }
        }

        public async Task LoginAsync(ProfileSummaryDTO profile, string country, string language, string currency)
        {
            try
            {
                LanguageId = language;
                CurrencyId = currency;
                ProfileId = profile?.SteamId;
                await SaveAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set state. {ex.Message}");
            }

            if (profile != null)
            {
                try
                {
                    // POST /profile/id
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to upate profile info. {ex.Message}");
                }
            }

            await RefreshAsync();
        }

        public long ToLocalPrice(long value, CurrencyDTO currency)
        {
            return value;
            /*
            var localCurrency = CurrencyLocal;
            var systemCurrency = CurrencySystem;
            var sourceCurrency = Currencies.FirstOrDefault(x => x.Name == currency?.Name);
            if (localCurrency == null || systemCurrency == null || sourceCurrency == null)
            {
                return 0;
            }

            decimal localValue = value;
            if (sourceCurrency != localCurrency)
            {
                decimal systemValue = value;
                if (sourceCurrency != systemCurrency)
                {
                    systemValue = (value > 0)
                        ? ((decimal)value / sourceCurrency.ExchangeRateMultiplier)
                        : 0;
                }

                localValue = (systemValue * localCurrency.ExchangeRateMultiplier);
            }

            return (long) Math.Floor(localValue);
            */
        }

        public string ToLocalPriceText(long value, CurrencyDTO currency)
        {
            var localCurrency = (this.Currency ?? currency);
            if (localCurrency == null)
            {
                return null;
            }

            return localCurrency?.ToPriceString(ToLocalPrice(value, currency));
        }
    }
}
