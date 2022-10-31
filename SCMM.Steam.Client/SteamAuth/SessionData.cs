using System.Net;

namespace SteamAuth
{
    public class SessionData
    {
        public string OAuthToken { get; set; }

        public ulong SteamID { get; set; }

        public string SessionID { get; set; }

        public string SteamLogin { get; set; }

        public string SteamLoginSecure { get; set; }

        public Cookie[] Cookies { get; set; }

        public void AddCookies(CookieContainer cookies)
        {
            if (Cookies?.Length > 0)
            {
                foreach(var cookie in Cookies)
                {
                    cookies.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain)
                    {
                        HttpOnly = cookie.HttpOnly,
                        Secure = cookie.Secure,
                        Expires = cookie.Expires                        
                    });
                }
            }

            cookies.Add(new Cookie("steamid", SteamID.ToString(), "/", "steamcommunity.com"));
            cookies.Add(new Cookie("steamLogin", SteamID + "%7C%7C" + SteamLogin, "/", "steamcommunity.com")
            {
                HttpOnly = true
            });
            cookies.Add(new Cookie("steamLoginSecure", SteamID + "%7C%7C" + SteamLoginSecure, "/", "steamcommunity.com")
            {
                HttpOnly = true,
                Secure = true
            });
        }
    }
}
