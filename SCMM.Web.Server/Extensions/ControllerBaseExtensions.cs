using Microsoft.AspNetCore.Mvc;
using SCMM.Steam.Data.Models;
using SCMM.Web.Data.Models.UI.Currency;
using SCMM.Web.Data.Models.UI.Language;

namespace SCMM.Web.Server.Extensions
{
    public static class ControllerBaseExtensions
    {
        public static string App(this ControllerBase controller)
        {
            // TODO: Make this configurable by the client
            return Constants.RustAppId.ToString();
        }

        public static LanguageDetailedDTO Language(this ControllerBase controller)
        {
            var languageName = AppState.DefaultLanguage;

            // If the language was specified in the request query, use that
            if (controller.Request.Query.ContainsKey(AppState.HttpHeaderLanguage))
            {
                languageName = controller.Request.Query[AppState.HttpHeaderLanguage].ToString();
            }
            // Else, if the language was specified in the request headers, use that
            else if (controller.Request.Headers.ContainsKey(AppState.HttpHeaderLanguage))
            {
                languageName = controller.Request.Headers[AppState.HttpHeaderLanguage].ToString();
            }
            // Else, if the user is authenticated and has a preferred language, use that
            else if (controller.User.Identity.IsAuthenticated && !string.IsNullOrEmpty(controller.User.Language()))
            {
                languageName = controller.User.Language();
            }

            return LanguageCache.GetByName(languageName) ??
                   LanguageCache.GetByName(AppState.DefaultLanguage);
        }

        public static CurrencyDetailedDTO Currency(this ControllerBase controller)
        {
            var currencyName = AppState.DefaultCurrency;

            // If the currency was specified in the request query, use that
            if (controller.Request.Query.ContainsKey(AppState.HttpHeaderCurrency))
            {
                currencyName = controller.Request.Query[AppState.HttpHeaderCurrency].ToString();
            }
            // Else, if the currency was specified in the request headers, use that
            else if (controller.Request.Headers.ContainsKey(AppState.HttpHeaderCurrency))
            {
                currencyName = controller.Request.Headers[AppState.HttpHeaderCurrency].ToString();
            }
            // Else, if the user is authenticated and has a preferred currency, use that
            else if (controller.User.Identity.IsAuthenticated && !string.IsNullOrEmpty(controller.User.Currency()))
            {
                currencyName = controller.User.Currency();
            }

            return CurrencyCache.GetByName(currencyName) ??
                   CurrencyCache.GetByName(AppState.DefaultCurrency);
        }
    }
}
