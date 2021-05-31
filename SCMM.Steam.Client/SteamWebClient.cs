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
using System.Runtime.Serialization;
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
                _logger.LogError(ex, $"GET '{request}' failed. {ex.Message}");
                throw new SteamRequestException($"GET '{request}' failed", ex);
            }

            return null;
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
                _logger.LogError(ex, $"GET '{request}' failed. {ex.Message}");
                throw new SteamRequestException($"GET '{request}' failed", ex);
            }

            return null;
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
                _logger.LogError(ex, $"GET '{request}' failed. {ex.Message}");
                throw new SteamRequestException($"GET '{request}' failed", ex);
            }

            return null;
        }

        public async Task<TResponse> GetXml<TRequest, TResponse>(TRequest request)
            where TRequest : SteamRequest
        {
            var xml = (string)null;
            try
            {
                xml = await GetText(request);
                if (!String.IsNullOrEmpty(xml))
                {
                    var xmlSerializer = new XmlSerializer(typeof(TResponse));
                    using var reader = new StringReader(xml);
                    var response = (TResponse)xmlSerializer.Deserialize(reader);
                    if (response != null)
                    {
                        return response;
                    }
                    else
                    {
                        throw new SerializationException("The response was empty");
                    }
                }
                else
                {
                    throw new HttpRequestException("The response was empty");
                }
            }
            catch (Exception ex)
            {
                if (!String.IsNullOrEmpty(xml))
                {
                    var xmlSerializer = new XmlSerializer(typeof(SteamErrorXmlResponse));
                    using var reader = new StringReader(xml);
                    var error = (SteamErrorXmlResponse) xmlSerializer.Deserialize(reader);
                    if (error != null)
                    {
                        throw new SteamRequestException(error.Error, ex, error);
                    }
                    else
                    {
                        _logger.LogError($"XML response could not be parsed (request: {typeof(TRequest)}, response: {typeof(TResponse)}, length: {xml?.Length ?? -1}).\n{xml}");
                        throw new SteamRequestException($"The response could not be parsed (request: {typeof(TRequest)}, response: {typeof(TResponse)}, length: {xml?.Length ?? -1})");
                    }
                }
                
                throw new SteamRequestException($"GET '{request}' failed", ex);
            }
        }

        public async Task<TResponse> GetJson<TRequest, TResponse>(TRequest request)
            where TRequest : SteamRequest
        {
            try
            {
                var json = await GetText(request);
                if (!String.IsNullOrEmpty(json))
                {
                    return JsonConvert.DeserializeObject<TResponse>(json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GET '{request}' failed. {ex.Message}");
                throw new SteamRequestException($"GET '{request}' failed", ex);
            }

            return default(TResponse);
        }
    }
}
