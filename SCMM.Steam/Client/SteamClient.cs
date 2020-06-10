using Newtonsoft.Json;
using SCMM.Steam.Shared.Community.Requests.Blob;
using SCMM.Steam.Shared.Community.Requests.Html;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Steam.Shared.Community.Responses.Json;
using SCMM.Steam.Shared.Community.Responses.Xml;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SCMM.Steam.Client
{
    // TODO: Add error proper handling for StatusCode: 429, ReasonPhrase: 'Too Many Requests'
    public class SteamClient
    {
        private readonly HttpClientHandler _httpHandler;

        public SteamClient(CookieContainer cookies = null)
        {
            _httpHandler = new HttpClientHandler()
            {
                UseCookies = (cookies != null),
                CookieContainer = (cookies ?? new CookieContainer())
            };
        }

        private HttpClient BuildSteamHttpClient(Uri uri)
        {
            return new HttpClient(_httpHandler, false)
            {
                BaseAddress = uri
            };
        }

        public async Task<SteamProfileXmlResponse> GetProfile(SteamProfilePageRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var xml = await response.Content.ReadAsStringAsync();
                var xmlSerializer = new XmlSerializer(typeof(SteamProfileXmlResponse));
                using (var reader = new StringReader(xml))
                {
                    return (SteamProfileXmlResponse)xmlSerializer.Deserialize(reader);
                }
            }
        }

        public async Task<SteamMarketAppFiltersJsonResponse> GetMarketAppFilters(SteamMarketAppFiltersJsonRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var filters = JsonConvert.DeserializeObject<SteamMarketAppFiltersJsonResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return filters;
            }
        }

        public async Task<SteamInventoryPaginatedJsonResponse> GetInventoryPaginated(SteamInventoryPaginatedJsonRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var inventory = JsonConvert.DeserializeObject<SteamInventoryPaginatedJsonResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return inventory;
            }
        }

        public async Task<SteamMarketMyListingsPaginatedJsonResponse> GetMarketMyListingsPaginated(SteamMarketMyListingsPaginatedJsonRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var listings = JsonConvert.DeserializeObject<SteamMarketMyListingsPaginatedJsonResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return listings;
            }
        }

        public async Task<SteamMarketMyHistoryPaginatedJsonResponse> GetMarketMyHistoryPaginated(SteamMarketMyHistoryPaginatedJsonRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var history = JsonConvert.DeserializeObject<SteamMarketMyHistoryPaginatedJsonResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return history;
            }
        }

        public async Task<string> GetMarketListingItemNameId(SteamMarketListingPageRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                // TODO: Find a better way to look this up...
                var html = await response.Content.ReadAsStringAsync();
                var itemNameIdMatchGroup = Regex.Match(html, @"Market_LoadOrderSpread\((.*)\)").Groups;
                var itemNameId = (itemNameIdMatchGroup.Count > 1) ? itemNameIdMatchGroup[1].Value.Trim() : null;
                return itemNameId;
            }
        }

        public async Task<SteamMarketSearchPaginatedJsonResponse> GetMarketSearchPaginated(SteamMarketSearchPaginatedJsonRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var search = JsonConvert.DeserializeObject<SteamMarketSearchPaginatedJsonResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return search;
            }
        }

        public async Task<SteamMarketItemOrdersActivityJsonResponse> GetMarketItemOrdersActivity(SteamMarketItemOrdersActivityJsonRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var activity = JsonConvert.DeserializeObject<SteamMarketItemOrdersActivityJsonResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return activity;
            }
        }

        public async Task<SteamMarketItemOrdersHistogramJsonResponse> GetMarketItemOrdersHistogram(SteamMarketItemOrdersHistogramJsonRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var histogram = JsonConvert.DeserializeObject<SteamMarketItemOrdersHistogramJsonResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return histogram;
            }
        }

        public async Task<SteamMarketPriceOverviewJsonResponse> GetMarketPriceOverview(SteamMarketPriceOverviewJsonRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var priceOverview = JsonConvert.DeserializeObject<SteamMarketPriceOverviewJsonResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return priceOverview;
            }
        }

        public async Task<SteamMarketPriceHistoryJsonResponse> GetMarketPriceHistory(SteamMarketPriceHistoryJsonRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var priceHistory = JsonConvert.DeserializeObject<SteamMarketPriceHistoryJsonResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return priceHistory;
            }
        }

        public async Task<byte[]> GetEconomyImage(SteamEconomyImageBlobRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var image = await response.Content.ReadAsByteArrayAsync();
                return image;
            }
        }
    }
}
