using System;
using System.Collections.Generic;
using System.Net;

namespace SCMM.Steam.Shared
{
    public class SteamConfiguration
    {
        public string ApplicationKey { get; set; }

        public IDictionary<string, IEnumerable<Cookie>> Cookies { get; set; }

        public CookieContainer GetCookieContainer()
        {
            var container = new CookieContainer();
            foreach (var cookieSet in Cookies)
            {
                var uri = new Uri(Uri.UnescapeDataString(cookieSet.Key));
                foreach (var cookie in cookieSet.Value)
                {
                    cookie.Domain = (String.IsNullOrEmpty(cookie.Domain) ? uri.Host : cookie.Domain);
                    cookie.Path = (String.IsNullOrEmpty(cookie.Path) ? "/" : cookie.Path);
                    cookie.Expires = (cookie.Expires == DateTime.MinValue ? DateTime.MaxValue : cookie.Expires);
                    container.Add(uri, cookie);
                }
            }
            return container;
        }
    }
}
