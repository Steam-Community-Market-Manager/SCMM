using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Responses.Xml;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SCMM.Steam.Client
{
    // TODO: Add error proper handling for StatusCode: 429, ReasonPhrase: 'Too Many Requests'
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

        protected HttpClient BuildSteamHttpClient(Uri uri)
        {
            return new HttpClient(_httpHandler, false)
            {
                BaseAddress = uri
            };
        }

        public async Task<Tuple<byte[], string>> GetBinary<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            try
            {
                using var client = BuildSteamHttpClient(request.Uri);
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"{response.StatusCode}: {response.ReasonPhrase}", null, response.StatusCode);
                }

                return new Tuple<byte[], string>(
                    await response.Content.ReadAsByteArrayAsync(), response.Content.Headers?.ContentType?.MediaType
                );
            }
            catch (Exception ex)
            {
                throw new SteamRequestException($"GET '{request}' failed. {ex.Message}", (ex as HttpRequestException)?.StatusCode, ex);
            }
        }

        public async Task<string> GetText<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            try
            {
                using var client = BuildSteamHttpClient(request.Uri);
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"{response.StatusCode}: {response.ReasonPhrase}", null, response.StatusCode);
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw new SteamRequestException($"GET '{request}' failed. {ex.Message}", (ex as HttpRequestException)?.StatusCode, ex);
            }
        }

        public async Task<XElement> GetHtml<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            try
            {
                using var client = BuildSteamHttpClient(request.Uri);
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"{response.StatusCode}: {response.ReasonPhrase}", null, response.StatusCode);
                }

                var html = await response.Content.ReadAsStringAsync();
                if (String.IsNullOrEmpty(html))
                {
                    return null;
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
            catch (Exception ex)
            {
                throw new SteamRequestException($"GET '{request}' failed. {ex.Message}", (ex as HttpRequestException)?.StatusCode, ex);
            }
        }

        public async Task<TResponse> GetXml<TRequest, TResponse>(TRequest request)
            where TRequest : SteamRequest
        {
            var xml = await GetText(request);
            if (!String.IsNullOrEmpty(xml))
            {
                try
                {
                    var xmlSerializer = new XmlSerializer(typeof(TResponse));
                    using var reader = new StringReader(xml);
                    return (TResponse)xmlSerializer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    var error = (SteamErrorXmlResponse)null;
                    try
                    {
                        // Check if the response is actually a Steam error
                        var xmlSerializer = new XmlSerializer(typeof(SteamErrorXmlResponse));
                        using var reader = new StringReader(xml);
                        error = (SteamErrorXmlResponse)xmlSerializer.Deserialize(reader);
                    }
                    finally
                    {
                        if (error != null)
                        {
                            throw new SteamRequestException($"GET '{request}' failed. {error.Message}", null, null, error);
                        }
                        else
                        {
                            throw new SteamRequestException($"GET '{request}' failed. {ex.Message}", null, ex);
                        }
                    }
                }
            }

            return default;
        }

        public async Task<TResponse> GetJson<TRequest, TResponse>(TRequest request)
            where TRequest : SteamRequest
        {
            var json = await GetText(request);
            if (!String.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonConvert.DeserializeObject<TResponse>(json);
                }
                catch (Exception ex)
                {
                    throw new SteamRequestException($"GET '{request}' failed. {ex.Message}", (ex as HttpRequestException)?.StatusCode, ex);
                }
            }

            return default;
        }
    }
}
