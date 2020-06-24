using Microsoft.Extensions.Logging;
using SCMM.Steam.Shared;
using SteamAuth;
using System;
using System.Net;

namespace SCMM.Steam.Client
{
    public class SteamSession
    {
        private readonly object _sessionLock = new object();
        private SessionData _session;

        public SteamSession(IServiceProvider serviceProvider)
        {
            Refresh(serviceProvider);
        }

        public void Refresh(IServiceProvider serviceProvider)
        {
            lock(_sessionLock)
            {
                var logger = serviceProvider.GetService(typeof(ILogger<SteamSession>)) as ILogger<SteamSession>;
                var config = serviceProvider.GetService(typeof(SteamConfiguration)) as SteamConfiguration;
                try
                {
                    logger.LogInformation($"Refreshing steam session data");
                    var login = new UserLogin(config.Username, config.Password);
                    var result = login.DoLogin();
                    if (result != LoginResult.LoginOkay)
                    {
                        logger.LogError($"Failed to login to steam (result: {result})");
                    }

                    logger.LogInformation($"Steam login result was {result}");
                    _session = login.Session;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error logging in to steam");
                    throw;
                }
            }
        }

        public CookieContainer Cookies 
        {
            get
            {
                lock (_sessionLock)
                {
                    var cookies = new CookieContainer();
                    _session?.AddCookies(cookies);
                    return cookies;
                }
            }
        }

        public bool IsValid => !String.IsNullOrEmpty(_session?.SteamLoginSecure);
    }
}
