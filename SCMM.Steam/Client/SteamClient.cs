using Newtonsoft.Json;
using SCMM.Steam.Shared.Requests.Community;
using SCMM.Steam.Shared.Responses.Community;
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

        public async Task<SteamProfileResponse> GetProfile(SteamProfileRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var xml = await response.Content.ReadAsStringAsync();
                var xmlSerializer = new XmlSerializer(typeof(SteamProfileResponse));
                using (var reader = new StringReader(xml))
                {
                    return (SteamProfileResponse)xmlSerializer.Deserialize(reader);
                }
            }
        }

        public async Task<SteamMarketAppFiltersResponse> GetInventoryPaginated(SteamMarketAppFiltersRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var filters = JsonConvert.DeserializeObject<SteamMarketAppFiltersResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return filters;
            }
        }

        public async Task<SteamInventoryPaginatedResponse> GetInventoryPaginated(SteamInventoryPaginatedRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var inventory = JsonConvert.DeserializeObject<SteamInventoryPaginatedResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return inventory;
            }
        }

        public async Task<SteamMarketHistoryPaginatedResponse> GetMarketHistoryPaginated(SteamMarketHistoryPaginatedRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var history = JsonConvert.DeserializeObject<SteamMarketHistoryPaginatedResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return history;
            }
        }

        public async Task<string> GetMarketListingItemNameId(SteamMarketListingRequest request)
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

        public async Task<SteamMarketSearchPaginatedResponse> GetMarketSearchPaginated(SteamMarketSearchPaginatedRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var search = JsonConvert.DeserializeObject<SteamMarketSearchPaginatedResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return search;
            }
        }

        public async Task<SteamMarketItemOrdersActivityResponse> GetMarketItemOrdersActivity(SteamMarketItemOrdersActivityRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var activity = JsonConvert.DeserializeObject<SteamMarketItemOrdersActivityResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return activity;
            }
        }

        public async Task<SteamMarketItemOrdersHistogramResponse> GetMarketItemOrdersHistogram(SteamMarketItemOrdersHistogramRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var histogram = JsonConvert.DeserializeObject<SteamMarketItemOrdersHistogramResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return histogram;
            }
        }

        public async Task<SteamMarketPriceOverviewResponse> GetMarketPriceOverview(SteamMarketPriceOverviewRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var priceOverview = JsonConvert.DeserializeObject<SteamMarketPriceOverviewResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return priceOverview;
            }
        }

        public async Task<SteamMarketPriceHistoryResponse> GetMarketPriceHistory(SteamMarketPriceHistoryRequest request)
        {
            using (var client = BuildSteamHttpClient(request.Uri))
            {
                var response = await client.GetAsync(request.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var priceHistory = JsonConvert.DeserializeObject<SteamMarketPriceHistoryResponse>(
                    await response.Content.ReadAsStringAsync()
                );

                return priceHistory;
            }
        }

        public async Task<byte[]> GetEconomyImage(SteamEconomyImageRequest request)
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
