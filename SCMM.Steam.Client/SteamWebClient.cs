using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Responses.Xml;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SCMM.Steam.Client
{
    public abstract class SteamWebClient : Worker.Client.WebClient
    {
        private readonly ILogger<SteamWebClient> _logger;
        private readonly IDistributedCache _cache;
        private readonly SteamSession _session;

        public SteamWebClient(ILogger<SteamWebClient> logger, IDistributedCache cache, SteamSession session = null) : base(cookieContainer: session?.Cookies)
        {
            _logger = logger;
            _cache = cache;
            _session = session;
        }

        public SteamSession Session => _session;

        private async Task<HttpResponseMessage> Post<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            using (var client = BuildHttpClient())
            {
                try
                {
                    var response = await client.PostAsync(request.Uri, null);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"{response.StatusCode}: {response.ReasonPhrase}", null, response.StatusCode);
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    throw new SteamRequestException($"POST '{request}' failed. {ex.Message}", (ex as HttpRequestException)?.StatusCode, ex);
                }
            }
        }

        private async Task<HttpResponseMessage> Get<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            using (var client = BuildHttpClient())
            {
                try
                {
                    var response = await client.GetAsync(request.Uri);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"{response.StatusCode}: {response.ReasonPhrase}", null, response.StatusCode);
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    throw new SteamRequestException($"GET '{request}' failed. {ex.Message}", (ex as HttpRequestException)?.StatusCode, ex);
                }
            }
        }

        private async Task<HttpResponseMessage> GetWithRetry<TRequest>(TRequest request, int attemptCount = 1)
            where TRequest : SteamRequest
        {
            try
            {
                // Retry up to three times, then give up
                if (attemptCount >= 3)
                {
                    throw new SteamRequestException($"Request failed after {attemptCount} attempts");
                }

                // Check if we need to add an delay to requests to stay within the rate-limit targets
                if (_session != null)
                {
                    // Add an artifical delay to requests based on the current rate-limit state
                    await _session.WaitForRateLimitDelaysAsync();
                }

                // Zhu Li, do the thing...
                var response = await Get(request);

                // Check if we are no longer rate limited
                if (_session != null && _session.IsRateLimited && response.StatusCode != HttpStatusCode.TooManyRequests)
                {
                    _session.SetRateLimited(false);
                }

                return response;
            }
            catch (SteamRequestException ex)
            {
                // Check if the request might have failed due to missing or expired authentication
                // 400: BadRequest
                // 401: Unauthorized
                // 403: Forbidden
                if (_session != null && ex.IsAuthenticiationRequired)
                {
                    // If it has been more than 10 minutes since we last authenticated, try login to Steam again. This error might just be that our token has expired.
                    if (_session.LastLoginOn == null || (DateTimeOffset.Now - _session.LastLoginOn.Value) >= TimeSpan.FromMinutes(10))
                    {
                        _session.Refresh();
                        return await GetWithRetry(request, attemptCount++);
                    }
                }

                // Check if the request failed due to rate limiting
                // 429: TooManyRequests
                else if (_session != null)
                {
                    if (!_session.IsRateLimited && ex.IsRateLimited)
                    {
                        // We are now rate limited, try again soon
                        _session.SetRateLimited(true);
                        return await GetWithRetry(request, attemptCount++);
                    }
                    else if (_session.IsRateLimited && !ex.IsRateLimited)
                    {
                        // We are no longer rate limited
                        _session.SetRateLimited(false);
                    }
                }

                throw;
            }
        }

        private async Task<string> GetStringCached<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            var content = (string)null;
            var cacheKey = $"http-requests:{request.Uri.Authority}{request.Uri.AbsolutePath.Replace('/', ':')}";
            if (!String.IsNullOrEmpty(request.Uri.Query))
            {
                cacheKey += $":{request.Uri.Query}";
            }
            
            // Try get the request from cache
            var cachedResponse = await _cache.GetAsync(cacheKey);
            if (cachedResponse != null)
            {
                content = Encoding.Unicode.GetString(cachedResponse);
            }
            else
            {
                // Cache miss, try get the request from the originating server
                var response = await GetWithRetry(request);
                content = await response.Content.ReadAsStringAsync();

                // Cache the response for next time (if any)
                if (!String.IsNullOrEmpty(content))
                {
                    _ = _cache.SetAsync(cacheKey, Encoding.Unicode.GetBytes(content), new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                    });
                }
            }

            // Steam has been known to sometimes put a null character at the end of the response :\
            return content?.Trim('\0'); 
        }

        public async Task<WebFileData> GetBinary<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            var response = await GetWithRetry(request);
            return new WebFileData()
            {
                Name = response.Content.Headers?.ContentDisposition?.FileName,
                MimeType = response.Content.Headers?.ContentType?.MediaType,
                Data = await response.Content.ReadAsByteArrayAsync()
            };
        }

        public async Task<string> GetText<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            return await GetStringCached(request);
        }

        public async Task<XElement> GetHtml<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            var html = await GetText(request);
            if (string.IsNullOrEmpty(html))
            {
                return default;
            }

            // Sanitise the html first to clean-up dodgy tags that may cause XML parsing to fail
            // (e.g. <meta>, <link>, etc)
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            var sanitisedHtml = new StringBuilder();
            using var stringWriter = new StringWriter(sanitisedHtml);
            var xmlWriter = new XmlTextWriter(stringWriter);
            htmlDocument.Save(xmlWriter);

            return XElement.Parse(sanitisedHtml.ToString());
        }

        public async Task<TResponse> GetXml<TRequest, TResponse>(TRequest request)
            where TRequest : SteamRequest
        {
            var xml = await GetText(request);
            if (string.IsNullOrEmpty(xml))
            {
                return default;
            }

            try
            {
                var xmlSerializer = new XmlSerializer(typeof(TResponse));
                using var reader = new StringReader(xml);
                return (TResponse)xmlSerializer.Deserialize(reader);
            }
            catch (Exception)
            {
                var steamError = (SteamErrorXmlResponse)null;
                try
                {
                    // Check if the response is actually a Steam error
                    var xmlSerializer = new XmlSerializer(typeof(SteamErrorXmlResponse));
                    using var reader = new StringReader(xml);
                    steamError = (SteamErrorXmlResponse)xmlSerializer.Deserialize(reader);
                }
                finally
                {
                    if (steamError != null)
                    {
                        throw new SteamRequestException($"GET '{request}' failed. {steamError.Message}", HttpStatusCode.OK, steamError);
                    }
                }

                throw;
            }
        }

        public async Task<TResponse> GetJson<TRequest, TResponse>(TRequest request)
            where TRequest : SteamRequest
        {
            var json = await GetText(request);
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<TResponse>(json);
        }

        public async Task<TResponse> PostJson<TRequest, TResponse>(TRequest request)
            where TRequest : SteamRequest
        {
            var response = await Post(request);
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<TResponse>(json);
        }
    }
}
