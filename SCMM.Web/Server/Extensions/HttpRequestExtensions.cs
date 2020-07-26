using Microsoft.AspNetCore.Http;
using SCMM.Web.Client;
using SCMM.Web.Server.Domain;
using SCMM.Web.Shared.Domain.DTOs.Currencies;
using SCMM.Web.Shared.Domain.DTOs.Languages;
using System.Linq;

namespace SCMM.Web.Server.API.Controllers.Extensions
{
    public static class HttpRequestExtensions
    {
        public static string LanguageId(this HttpRequest request)
        {
            return request.Headers.FirstOrDefault(x => x.Key == AppState.HttpHeaderLanguage).Value;
        }

        public static LanguageDetailedDTO Language(this HttpRequest request)
        {
            return SteamLanguageService.GetByNameCached(request.LanguageId());
        }

        public static string CurrencyId(this HttpRequest request)
        {
            return request.Headers.FirstOrDefault(x => x.Key == AppState.HttpHeaderCurrency).Value;
        }

        public static CurrencyDetailedDTO Currency(this HttpRequest request)
        {
            return SteamCurrencyService.GetByNameCached(request.CurrencyId());
        }

        public static string ProfileId(this HttpRequest request)
        {
            return request.Headers.FirstOrDefault(x => x.Key == AppState.HttpHeaderProfile).Value;
        }
    }
}
