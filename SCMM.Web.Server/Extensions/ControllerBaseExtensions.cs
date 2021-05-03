using Microsoft.AspNetCore.Mvc;
using SCMM.Web.Client;
using SCMM.Web.Data.Models.Domain.Currencies;
using SCMM.Web.Data.Models.Domain.Languages;

namespace SCMM.Web.Server.Extensions
{
    public static class ControllerBaseExtensions
    {
        public static string App(this ControllerBase controller)
        {
            // TODO: Make this configurable by the client
            return "252490"; // Rust"
        }

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

            return LanguageCache.GetByName(languageName) ??
                   LanguageCache.GetByName(AppState.DefaultLanguage);
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

            return CurrencyCache.GetByName(currencyName) ??
                   CurrencyCache.GetByName(AppState.DefaultCurrency);
        }
    }
}
