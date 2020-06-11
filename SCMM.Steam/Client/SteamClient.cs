using System;
using System.Net;
using System.Net.Http;

namespace SCMM.Steam.Client
{
    // TODO: Add error proper handling for StatusCode: 429, ReasonPhrase: 'Too Many Requests'
    public abstract class SteamClient
    {
        private readonly HttpClientHandler _httpHandler;

        public SteamClient(CookieContainer cookies = null)
        {
            _httpHandler = new HttpClientHandler()
            {
                UseCookies = (cookies != null),
                CookieContainer = (cookies ?? new CookieContainer())
            };
        }

        protected HttpClient BuildSteamHttpClient(Uri uri)
        {
            return new HttpClient(_httpHandler, false)
            {
                BaseAddress = uri
            };
        }
    }
}
