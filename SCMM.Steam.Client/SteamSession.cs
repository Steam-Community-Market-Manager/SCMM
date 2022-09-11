using Microsoft.Extensions.Logging;
using SteamAuth;
using System.Net;

namespace SCMM.Steam.Client
{
    public class SteamSession
    {
        private readonly ILogger<SteamSession> _logger;
        private readonly SteamConfiguration _configuration;
        private readonly CookieContainer _cookies;
        private readonly object _lock = new object();
        private SessionData _session;

        public SteamSession(ILogger<SteamSession> logger, SteamConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _cookies = new CookieContainer();
        }

        public void Refresh()
        {
            if (_cookies.Count > 0)
            {
                _logger.LogWarning("Steam session authentication is stale, attempting to refresh and try again...");
            }
            if (String.IsNullOrEmpty(_configuration?.Username) || String.IsNullOrEmpty(_configuration?.Password))
            {
                _logger.LogWarning("Unable to refresh Steam session cookies, no username/password was specified");
                return;
            }

            lock (_lock)
            {
                try
                {
                    // Refresh session
                    _logger.LogTrace($"Refreshing Steam session cookies...");
                    var login = new UserLogin(_configuration.Username, _configuration.Password);
                    var result = login.DoLogin();
                    if (result == LoginResult.LoginOkay)
                    {
                        _logger.LogTrace($"Steam login was successful (result: {result})");
                        _session = login.Session;
                    }
                    else
                    {
                        _logger.LogError($"Steam login was unsuccessful (result: {result})");
                    }

                    // Refresh cookies
                    if (_session != null)
                    {
                        _session.AddCookies(_cookies);
                    }

                    // Reset rate-limit state
                    RateLimitedSince = null;
                    RateLimitRequestCount = 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Steam login was unsuccessful");
                }
            }
        }

        public void SetRateLimited(bool isRateLimited)
        {
            if (isRateLimited)
            {
                _logger.LogWarning($"Steam session has entered rate-limit mode after {RateLimitRequestCount} requests");
                RateLimitedSince = DateTimeOffset.Now;
            }
            else
            {
                _logger.LogWarning($"Steam session has exited rate-limit mode after {(DateTimeOffset.Now - RateLimitedSince.Value).TotalSeconds} seconds");
                RateLimitedSince = null;
                RateLimitRequestCount = 0;
            }
        }

        public Task WaitForRateLimitDelaysAsync()
        {
            if (RateLimitedSince != null)
            {
                return Task.Delay(TimeSpan.FromSeconds(30));
            }
            else
            {
                return Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }

        public CookieContainer Cookies
        {
            get
            {
                lock (_lock)
                {
                    // If we haven't got any cookies, try refreshing the session first
                    if (_cookies.Count == 0)
                    {
                        Refresh();
                    }
                }
                return _cookies;
            }
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(_session?.SteamLoginSecure);

        public bool IsRateLimited => RateLimitedSince != null;

        public DateTimeOffset? RateLimitedSince { get; set; }

        public int RateLimitRequestCount { get; set; }
    }
}
