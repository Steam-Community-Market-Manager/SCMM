using Microsoft.AspNetCore.Mvc;
using SCMM.Web.Data.Models.UI.Currency;
using SCMM.Web.Data.Models.UI.Language;
using SCMM.Web.Data.Models.UI.App;

namespace SCMM.Web.Server.Extensions
{
    public static class ControllerBaseExtensions
    {
        public static AppDetailedDTO App(this ControllerBase controller)
        {
            var appId = (ulong)0;

            // Get the app by the URL hostname or from the request info
            // NOTE: Navigating directly to the app hostname trumps all other request settings
            var hostApp = AppCache.GetByHostname(controller.Request.Host.Host);
            if (hostApp != null)
            {
                return hostApp;
            }

            // If the app was specified in the request query string, use that
            if (controller.Request.Query.ContainsKey(AppState.AppIdKey))
            {
                UInt64.TryParse(controller.Request.Query[AppState.AppIdKey].ToString(), out appId);
            }
            // Else, if the app was specified in the request headers, use that
            else if (controller.Request.Headers.ContainsKey(AppState.AppIdKey))
            {
                UInt64.TryParse(controller.Request.Headers[AppState.AppIdKey].ToString(), out appId);
            }
            // Else, if the app was specified in the request cookies, use that
            else if (controller.Request.Cookies.Any(x => String.Equals(x.Key, AppState.AppIdKey, StringComparison.InvariantCultureIgnoreCase)))
            {
                UInt64.TryParse(controller.Request.Cookies.FirstOrDefault(x => String.Equals(x.Key, AppState.AppIdKey, StringComparison.InvariantCultureIgnoreCase)).Value, out appId);
            }
            // Else, if the user is authenticated and has a preferred app, use that
            else if (controller.User.Identity.IsAuthenticated && !string.IsNullOrEmpty(controller.User.AppId()))
            {
                UInt64.TryParse(controller.User.AppId(), out appId);
            }

            return AppCache.GetById(appId) ?? 
                   AppCache.GetById(AppState.DefaultAppId);
        }

        public static LanguageDetailedDTO Language(this ControllerBase controller)
        {
            var languageName = (string)null;

            // If the language was specified in the request query string, use that
            if (controller.Request.Query.ContainsKey(AppState.LanguageNameKey))
            {
                languageName = controller.Request.Query[AppState.LanguageNameKey].ToString();
            }
            // Else, if the language was specified in the request headers, use that
            else if (controller.Request.Headers.ContainsKey(AppState.LanguageNameKey))
            {
                languageName = controller.Request.Headers[AppState.LanguageNameKey].ToString();
            }
            // Else, if the language was specified in the request cookies, use that
            else if (controller.Request.Cookies.Any(x => String.Equals(x.Key, AppState.LanguageNameKey, StringComparison.InvariantCultureIgnoreCase)))
            {
                languageName = controller.Request.Cookies.FirstOrDefault(x => String.Equals(x.Key, AppState.LanguageNameKey, StringComparison.InvariantCultureIgnoreCase)).Value;
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
            var currencyName = (string)null;

            // If the currency was specified in the request query string, use that
            if (controller.Request.Query.ContainsKey(AppState.CurrencyNameKey))
            {
                currencyName = controller.Request.Query[AppState.CurrencyNameKey].ToString();
            }
            // Else, if the currency was specified in the request headers, use that
            else if (controller.Request.Headers.ContainsKey(AppState.CurrencyNameKey))
            {
                currencyName = controller.Request.Headers[AppState.CurrencyNameKey].ToString();
            }
            // Else, if the currency was specified in the request cookies, use that
            else if (controller.Request.Cookies.Any(x => String.Equals(x.Key, AppState.CurrencyNameKey, StringComparison.InvariantCultureIgnoreCase)))
            {
                currencyName = controller.Request.Cookies.FirstOrDefault(x => String.Equals(x.Key, AppState.CurrencyNameKey, StringComparison.InvariantCultureIgnoreCase)).Value;
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
