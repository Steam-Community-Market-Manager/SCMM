using Blazored.LocalStorage;
using SCMM.Web.Shared;
using SCMM.Web.Shared.Domain.DTOs;
using SCMM.Web.Shared.Domain.DTOs.Currencies;
using System;
using System.Collections;
using System.Collections.Generic;
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
                if (!String.IsNullOrEmpty(localCurrencyId))
                {
                    CurrencyLocal = Currencies?.FirstOrDefault(x => x.SteamId == localCurrencyId);
                }
                if (CurrencyLocal == null)
                {
                    CurrencyLocal = await TryGuessLocalCurrency();
                    await SetLocalCurrency(CurrencyLocal);
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

        public long ToLocalPrice(long value, CurrencyDTO currency)
        {
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
        }

        public string ToLocalPriceText(long value, CurrencyDTO currency)
        {
            var localCurrency = CurrencyLocal;
            if (localCurrency == null)
            {
                return null;
            }

            return localCurrency.ToPriceString(ToLocalPrice(value, currency));
        }

        public async Task<CurrencyDetailsDTO> TryGuessLocalCurrency()
        {
            try
            {
                var country = await _http.GetStringAsync("https://ipinfo.io/country");
                if (String.IsNullOrEmpty(country))
                {
                    return null;
                }

                var countryCurrencyTable = await _http.GetFromJsonAsync<IDictionary<string, string>>("/json/country-currency.json");
                if (countryCurrencyTable == null)
                {
                    return null;
                }

                var currencyName = countryCurrencyTable.FirstOrDefault(x => x.Key == country.Trim()).Value;
                if (String.IsNullOrEmpty(currencyName))
                {
                    return null;
                }

                var currency = Currencies.FirstOrDefault(x => x.Name == currencyName);
                Console.WriteLine($"Auto-detected currency: '{currency?.Name}'");
                return currency ?? CurrencySystem;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to auto-detect currency. Error: {ex.Message}");
                return CurrencySystem;
            }
        }
    }
}
