using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SCMM.Shared.Data.Models;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Responses.Xml;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SCMM.Steam.Client
{
    public class SteamWebClient
    {
        private readonly ILogger<SteamWebClient> _logger;
        private readonly HttpClientHandler _httpHandler;
        private readonly SteamSession _session;

        public SteamWebClient(ILogger<SteamWebClient> logger, SteamSession session)
        {
            _logger = logger;
            _session = session;
            _httpHandler = new HttpClientHandler()
            {
                UseCookies = (session?.Cookies != null),
                CookieContainer = (session?.Cookies ?? new CookieContainer())
            };
        }

        public SteamSession Session => _session;

        private async Task<HttpResponseMessage> Get<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            using (var client = new HttpClient(_httpHandler, false))
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
                return await Get(request);
            }
            catch (SteamRequestException ex)
            {
                if (ex.IsSessionStale)
                {
                    _logger.LogWarning("Steam session is stale, attempting to refresh and try again...");
                    _session.Refresh();
                    return await Get(request);
                }
                else if (ex.IsThrottled)
                {
                    _logger.LogWarning("Steam session is throttled, need to back off...");
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
            return await response.Content.ReadAsStringAsync();
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

            return JsonConvert.DeserializeObject<TResponse>(json);
        }
    }
}
