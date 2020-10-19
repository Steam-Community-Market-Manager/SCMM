using Microsoft.AspNetCore.Mvc;
using SCMM.Web.Client;
using SCMM.Web.Server.Services;
using SCMM.Web.Shared.Domain.DTOs.Currencies;
using SCMM.Web.Shared.Domain.DTOs.Languages;

namespace SCMM.Web.Server.Extensions
{
    public static class ControllerBaseExtensions
    {
        public static LanguageDetailedDTO Language(this ControllerBase controller)
        {
            var languageName = AppState.DefaultLanguage;

            // If the user is authenticated, use their preferred language (if any)
            if (controller.User.Identity.IsAuthenticated && !string.IsNullOrEmpty(controller.User.Language()))
            {
                languageName = controller.User.Language();
            }
            // If the language was specified in the request headers, use that
            else if (controller.Request.Headers.ContainsKey(AppState.HttpHeaderLanguage))
            {
                languageName = controller.Request.Headers[AppState.HttpHeaderLanguage].ToString();
            }

            return SteamLanguageService.GetByNameCached(languageName);
        }

        public static CurrencyDetailedDTO Currency(this ControllerBase controller)
        {
            var currencyName = AppState.DefaultCurrency;

            // If the user is authenticated, use their preferred currency (if any)
            if (controller.User.Identity.IsAuthenticated && !string.IsNullOrEmpty(controller.User.Currency()))
            {
                currencyName = controller.User.Currency();
            }
            // If the currency was specified in the request headers, use that
            else if (controller.Request.Headers.ContainsKey(AppState.HttpHeaderCurrency))
            {
                currencyName = controller.Request.Headers[AppState.HttpHeaderCurrency].ToString();
            }

            return SteamCurrencyService.GetByNameCached(currencyName);
        }
    }
}
