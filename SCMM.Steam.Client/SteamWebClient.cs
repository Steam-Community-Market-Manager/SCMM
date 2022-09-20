using HtmlAgilityPack;
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
        private readonly SteamSession _session;

        public SteamWebClient(ILogger<SteamWebClient> logger, SteamSession session) : base(cookieContainer: session?.Cookies)
        {
            _logger = logger;
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

        private async Task<HttpResponseMessage> GetWithRetry<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            try
            {
                if (_session != null)
                {
                    // Add an artifical delay to requests based on the current rate-limit state
                    await _session.WaitForRateLimitDelaysAsync();
                }

                // Zhu Li, do the thing...
                var response = await Get(request);

                if (_session != null && _session.IsRateLimited && response.IsSuccessStatusCode)
                {
                    // Success, we're no longer rate limited :)
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
                if (ex.IsAuthenticiationRequired && _session != null)
                {
                    // If it has been more than 10 minutes since we last authenticated, try login to Steam again. This error might just be that our token has expired.
                    if (_session.LastLoginOn == null || (DateTimeOffset.Now - _session.LastLoginOn.Value) >= TimeSpan.FromMinutes(10))
                    {
                        _session.Refresh();
                        return await Get(request);
                    }
                }
                // Check if the request failed due to rate limiting
                // 429: TooManyRequests
                else if (ex.IsRateLimited && _session != null)
                {
                    // Back-off requests to avoid getting rate-limited more
                    _session.SetRateLimited(true);
                }

                throw;
            }
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
            var response = await GetWithRetry(request);
            var text = await response.Content.ReadAsStringAsync();
            return text?.Trim('\0'); // Steam has been known to sometimes put a null character at the end of the response :\
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
