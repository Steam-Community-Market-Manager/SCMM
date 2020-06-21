using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SCMM.Steam.Shared;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SCMM.Steam.Client
{
    // TODO: Add error proper handling for StatusCode: 429, ReasonPhrase: 'Too Many Requests'
    public abstract class SteamClient
    {
        private readonly ILogger<SteamClient> _logger;
        private readonly HttpClientHandler _httpHandler;
        private readonly SteamSession _session;

        public SteamClient(ILogger<SteamClient> logger, SteamSession session)
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

        protected async Task<byte[]> GetBinary<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            try
            {
                using (var client = BuildSteamHttpClient(request.Uri))
                {
                    var response = await client.GetAsync(request.Uri);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"{response.StatusCode}: {response.ReasonPhrase}");
                    }

                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GET '{request.Uri}' failed");
            }

            return null;
        }

        protected async Task<string> GetText<TRequest>(TRequest request)
            where TRequest : SteamRequest
        {
            try
            {
                using (var client = BuildSteamHttpClient(request.Uri))
                {
                    var response = await client.GetAsync(request.Uri);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"{response.StatusCode}: {response.ReasonPhrase}");
                    }

                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GET '{request.Uri}' failed");
            }

            return null;
        }

        protected async Task<TResponse> GetXml<TRequest, TResponse>(TRequest request)
            where TRequest : SteamRequest
        {
            try
            {
                var xml = await GetText(request);
                if (!String.IsNullOrEmpty(xml))
                {
                    var xmlSerializer = new XmlSerializer(typeof(TResponse));
                    using (var reader = new StringReader(xml))
                    {
                        return (TResponse)xmlSerializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GET '{request.Uri}' failed");
            }

            return default(TResponse);
        }

        protected async Task<TResponse> GetJson<TRequest, TResponse>(TRequest request)
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
                _logger.LogError(ex, $"GET '{request.Uri}' failed");
            }

            return default(TResponse);
        }
    }
}
