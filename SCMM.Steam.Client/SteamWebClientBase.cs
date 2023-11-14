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
    public abstract class SteamWebClientBase : Shared.Web.Client.WebClientBase
    {
        private readonly ILogger<SteamWebClientBase> _logger;
        private readonly IDistributedCache _cache;

        public SteamWebClientBase(ILogger<SteamWebClientBase> logger, IDistributedCache cache, IWebProxy proxy = null) : base(logger, webProxy: proxy)
        {
            _logger = logger;
            _cache = cache;

            // Steam web API terms of use (https://steamcommunity.com/dev/apiterms)
            //  - You are limited to one hundred thousand (100,000) calls to the Steam Web API per day.
            // Steam community web site rate-limits observed from personal testing:
            //  - You are limited to 25 requests within 30 seconds, which resets after 30 minutes?
            // TODO: Find a reputable source for Steam rate limit rules and update this.
            RateLimitCooldown = TimeSpan.FromMinutes(30);
        }

        private string SafeUrl(string requestUrl)
        {
            return Regex.Replace(requestUrl, @"key=([^&]*)", "key=***************", RegexOptions.IgnoreCase);
        }

        private async Task<HttpResponseMessage> PostWithRetryAsync<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            using (var client = BuildHttpClient())
            {
                try
                {
                    var formData = (request as SteamFormDataRequest);
                    var response = ((formData != null)
                        ? await RetryPolicy.ExecuteAsync(() => client.PostAsync(request.Uri, formData))
                        : await RetryPolicy.ExecuteAsync(() => client.PostAsync(request.Uri, null))
                    );
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"{response.StatusCode}: {response.ReasonPhrase}", null, response.StatusCode);
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    throw new SteamRequestException($"POST '{SafeUrl(request)}' failed. {ex.Message}", (ex as HttpRequestException)?.StatusCode, ex);
                }
            }
        }

        private async Task<HttpResponseMessage> GetWithRetryAsync<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            using (var client = BuildHttpClient())
            {
                try
                {
                    var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(request.Uri));
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
                    if (statusCode == HttpStatusCode.NotModified)
                    {
                        throw new SteamNotModifiedException();
                    }

                    throw new SteamRequestException($"GET '{SafeUrl(request)}' failed. {ex.Message}", statusCode, ex);
                }
                catch (Exception ex)
                {
                    throw new SteamRequestException($"GET '{SafeUrl(request)}' failed. {ex.Message}", null, ex);
                }
            }
        }

        private async Task<string> GetStringAsync<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            var response = await GetWithRetryAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<string> GetStringCachedAsync<TRequest>(TRequest request)
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
                content = await GetStringAsync(request);

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

        public async Task<WebFileData> GetBinaryAsync<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            var response = await GetWithRetryAsync(request);
            return new WebFileData()
            {
                Name = response.Content.Headers?.ContentDisposition?.FileName,
                MimeType = response.Content.Headers?.ContentType?.MediaType,
                Data = await response.Content.ReadAsByteArrayAsync()
            };
        }

        public async Task<string> GetTextAsync<TRequest>(TRequest request, bool useCache = false)
            where TRequest : SteamRequest
        {
            var text = useCache
                ? await GetStringCachedAsync(request)
                : await GetStringAsync(request);

            // Steam has been known to sometimes put a null character at the end of the response, trim it
            return text?.Trim('\0');
        }

        public async Task<XElement> GetHtmlAsync<TRequest>(TRequest request, bool useCache = false)
            where TRequest : SteamRequest
        {
            var html = await GetTextAsync(request, useCache);
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

        public async Task<TResponse> GetXmlAsync<TRequest, TResponse>(TRequest request, bool useCache = false)
            where TRequest : SteamRequest
        {
            var xml = await GetTextAsync(request, useCache);
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
                        throw new SteamRequestException($"GET '{SafeUrl(request)}' failed. {steamError.Message}", HttpStatusCode.OK, steamError);
                    }
                }

                throw;
            }
        }

        public async Task<TResponse> GetJsonAsync<TRequest, TResponse>(TRequest request, bool useCache = false)
            where TRequest : SteamRequest
        {
            var json = await GetTextAsync(request, useCache);
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<TResponse>(json);
        }

        public async Task<TResponse> PostJsonAsync<TRequest, TResponse>(TRequest request)
            where TRequest : SteamRequest
        {
            var response = await PostWithRetryAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<TResponse>(json);
        }
    }
}
