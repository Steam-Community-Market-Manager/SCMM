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
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SCMM.Steam.Client
{
    public abstract class SteamWebClient : Shared.Client.WebClient
    {
        private readonly ILogger<SteamWebClient> _logger;
        private readonly IDistributedCache _cache;
        private readonly SteamSession _session;

        public SteamWebClient(ILogger<SteamWebClient> logger, IDistributedCache cache, SteamSession session = null, IWebProxy proxy = null) : base(cookieContainer: session?.Cookies, proxy)
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
                catch (HttpRequestException ex)
                {
                    var statusCode = ex.StatusCode ?? 0;
                    if (statusCode == 0)
                    {
                        var extractedStatusCode = Regex.Match(ex.Message, @"status code '([0-9]{3})'").Groups.OfType<Capture>().LastOrDefault()?.Value;
                        if (!String.IsNullOrEmpty(extractedStatusCode))
                        {
                            Enum.TryParse<HttpStatusCode>(extractedStatusCode, true, out statusCode);
                        }
                    }

                    throw new SteamRequestException($"GET '{request}' failed. {ex.Message}", statusCode, ex);
                }
                catch (Exception ex)
                {
                    throw new SteamRequestException($"GET '{request}' failed. {ex.Message}", null, ex);
                }
            }
        }

        private async Task<HttpResponseMessage> GetWithRetry<TRequest>(TRequest request, int retryAttempt = 0)
            where TRequest : SteamRequest
        {
            try
            {
                // Retry up to 5 times, then give up
                if (retryAttempt >= 5)
                {
                    throw new SteamRequestException($"Request failed after {retryAttempt} attempts");
                }

                // Use a back-off delay between retry attempts to avoid further rate-limiting
                if (retryAttempt > 1)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(3, retryAttempt));
                    _logger.LogWarning($"Request is delaying for {delay.TotalSeconds} seconds (retry attempt: {retryAttempt})");
                    await Task.Delay(delay);
                }

                // Zhu Li, do the thing...
                var response = await Get(request);
                if (response != null)
                {
                    UpdateRequestStatistics(request?.Uri, response.StatusCode);
                }

                return response;
            }
            catch (SteamRequestException ex)
            {
                if (ex.StatusCode != null)
                {
                    UpdateRequestStatistics(request?.Uri, ex.StatusCode.Value);
                }

                // Check if the content has not been modified since the last request
                // 304: Not Modified
                if (ex.IsNotModified)
                {
                    throw new SteamNotModifiedException();
                }

                // Check if the request failed due to a temporary or network related error
                // 408: RequestTimeout
                // 504: GatewayTimeout
                // 502: BadGateway
                if (ex.IsTemporaryError)
                {
                    _logger.LogWarning($"{ex.StatusCode} ({((int)ex.StatusCode)}), will retry...");
                    return await GetWithRetry(request, (retryAttempt + 1));
                }

                // Check if the request failed due to rate limiting
                // 429: TooManyRequests
                if (ex.IsRateLimited)
                {
                    // Add a cooldown to the current web proxy and rotate to the next proxy if possible
                    // Steam web API terms of use (https://steamcommunity.com/dev/apiterms)
                    //  - You are limited to one hundred thousand (100,000) calls to the Steam Web API per day.
                    // Steam community web site rate-limits observed from personal testing:
                    //  - You are limited to 20 requests within 20 seconds, which resets after 60 seconds.
                    RotateWebProxy(request?.Uri, cooldown: TimeSpan.FromMinutes(60));
                    return await GetWithRetry(request, (retryAttempt + 1));
                }

                // Check if the request failed due to missing proxy authentication
                // 407: ProxyAuthenticationRequired
                if (ex.IsProxyAuthenticationRequired)
                {
                    // Disable the current web proxy and rotate to the next proxy if possible
                    DisableWebProxy(request?.Uri);
                    return await GetWithRetry(request, (retryAttempt + 1));
                }

                // Check if the request might have failed due to missing or expired Steam authentication
                // 400: BadRequest
                // 401: Unauthorized
                // 403: Forbidden
                if (ex.IsSteamAuthenticationRequired && _session != null)
                {
                    // If it has been more than 10 minutes since we last authenticated, try login to Steam again. This error might just be that our token has expired.
                    if (_session.LastLoginOn == null || (DateTimeOffset.Now - _session.LastLoginOn.Value) >= TimeSpan.FromMinutes(10))
                    {
                        _session.Refresh();
                        return await GetWithRetry(request, (retryAttempt + 1));
                    }
                }

                // Legitimate error, bubble the error up to the caller
                throw;
            }
        }

        private async Task<string> GetString<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            var response = await GetWithRetry(request);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<string> GetStringCached<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            var content = (string)null;
            var cacheKey = $"http-requests:{request.Uri.Authority}:{request.Uri.AbsolutePath.Trim('/').Replace('/', ':')}";
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
                content = await GetString(request);

                // Cache the response for next time (if any)
                if (!String.IsNullOrEmpty(content))
                {
                    _ = _cache.SetAsync(cacheKey, Encoding.Unicode.GetBytes(content), new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                    });
                }
            }

            return content;
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

        public async Task<string> GetText<TRequest>(TRequest request, bool useCache = false)
            where TRequest : SteamRequest
        {
            var text = useCache
                ? await GetStringCached(request)
                : await GetString(request);

            // Steam has been known to sometimes put a null character at the end of the response, trim it
            return text?.Trim('\0');
        }

        public async Task<XElement> GetHtml<TRequest>(TRequest request, bool useCache = false)
            where TRequest : SteamRequest
        {
            var html = await GetText(request, useCache);
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

        public async Task<TResponse> GetXml<TRequest, TResponse>(TRequest request, bool useCache = false)
            where TRequest : SteamRequest
        {
            var xml = await GetText(request, useCache);
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

        public async Task<TResponse> GetJson<TRequest, TResponse>(TRequest request, bool useCache = false)
            where TRequest : SteamRequest
        {
            var json = await GetText(request, useCache);
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
