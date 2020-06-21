using Microsoft.Extensions.Logging;
using SCMM.Steam.Shared;
using SteamAuth;
using System;
using System.Net;

namespace SCMM.Steam.Client
{
    public class SteamSession
    {
        private readonly SessionData _session;

        public SteamSession(IServiceProvider serviceProvider)
        {
            _session = LoginAndGetSessionData(serviceProvider);
        }

        private SessionData LoginAndGetSessionData(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetService(typeof(ILogger<SteamSession>)) as ILogger<SteamSession>;
            var config = serviceProvider.GetService(typeof(SteamConfiguration)) as SteamConfiguration;
            try
            {
                var login = new UserLogin(config.Username, config.Password);
                var result = login.DoLogin();
                if (result != LoginResult.LoginOkay)
                {
                    logger.LogError($"Failed to login to steam (result: {result})");
                }

                logger.LogInformation("Logged in to steam ok");
                return login.Session;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to login to steam");
                throw;
            }
        }

        public CookieContainer Cookies 
        {
            get
            {
                var cookies = new CookieContainer();
                _session?.AddCookies(cookies);
                return cookies;
            }
        }

        public bool IsValid => !String.IsNullOrEmpty(_session.SteamLoginSecure);
    }
}
