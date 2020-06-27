using Blazored.LocalStorage;
using SCMM.Web.Shared;
using SCMM.Web.Shared.Domain.DTOs;
using SCMM.Web.Shared.Domain.DTOs.Currencies;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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

            ReloadCurrencies();
        }

        public ProfileDTO Profile { get; set; }

        public CurrencyDetailsDTO[] Currencies { get; set; }

        public CurrencyDetailsDTO CurrencySystem { get; set; }

        public CurrencyDetailsDTO CurrencyLocal { get; set; }

        public event EventHandler<CurrencyDetailsDTO[]> OnCurrenciesChanged;

        public async Task ReloadCurrencies()
        {
            Currencies = await _http.GetFromJsonAsync<CurrencyDetailsDTO[]>($"Currency");
            if (CurrencySystem == null)
            {
                CurrencySystem = Currencies?.FirstOrDefault(x => x.IsDefault);
            }
            if (CurrencyLocal == null)
            {
                var localCurrencyId = await _storage.GetItemAsync<string>("currency");
                if (!string.IsNullOrEmpty(localCurrencyId))
                {
                    CurrencyLocal = Currencies?.FirstOrDefault(x => x.SteamId == localCurrencyId);
                }
                if (CurrencyLocal == null)
                {
                    CurrencyLocal = CurrencySystem;
                }
            }

            OnCurrenciesChanged?.Invoke(this, Currencies);
        }

        public async Task SetLocalCurrency(CurrencyDetailsDTO currency)
        {
            CurrencyLocal = currency;
            if (currency != null)
            {
                await _storage.SetItemAsync<string>("currency", currency.SteamId);
            }
            else
            {
                await _storage.RemoveItemAsync("currency");
            }
        }

        public string ToLocalPriceText(long value, CurrencyDTO currency)
        {
            var localCurrency = CurrencyLocal;
            var systemCurrency = CurrencySystem;
            var sourceCurrency = Currencies.FirstOrDefault(x => x.Name == currency.Name);
            if (localCurrency == null || systemCurrency == null || sourceCurrency == null)
            {
                return null;
            }

            decimal localValue = value;
            if (sourceCurrency != localCurrency)
            {
                decimal systemValue = value;
                if (sourceCurrency != systemCurrency)
                {
                    systemValue = (value > 0)
                        ? (value / sourceCurrency.ExchangeRateMultiplier)
                        : 0;
                }

                localValue = (systemValue * localCurrency.ExchangeRateMultiplier);
            }

            return localCurrency.ToPriceString((long) Math.Floor(localValue));
        }
    }
}
