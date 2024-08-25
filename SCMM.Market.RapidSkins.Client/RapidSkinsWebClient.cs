using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace SCMM.Market.RapidSkins.Client
{
    public class RapidSkinsWebClient : Shared.Web.Client.WebClientBase
    {
        private const string WebBaseUri = "https://rapidskins.com/";
        private const string ApiBaseUri = "https://api.rapidskins.com/";

        public RapidSkinsWebClient(ILogger<RapidSkinsWebClient> logger) : base(logger) { }

        public async Task<RapidSkinsPaginatedItems> GetSiteInventoryAsync(string appId, int page = 1)
        {
            using (var client = BuildWebApiHttpClient(host: new Uri(ApiBaseUri)))
            {
                var url = $"{ApiBaseUri}graphql";
                var request = new RapidSkinsGraphQLRequest()
                {
                    OperationName = "Inventories",
                    Query = @"
                        query Inventories($filter: InventoryFilters!) { 
                          siteInventory(filter: $filter) {
                            ...InventoryFragment
                          }
                        }
                        fragment InventoryFragment on CompleteInventory {
                          rust {
                            ... on SteamInventory {
                              lastPage
                              items {
                                ownerSteamId
                                appId
                                marketHashName
                                price {
                                  coinAmount
                                }
                                stack {
                                  assetId
                                  amount
                                }
                              }
                            }
                          }
                        }
                    ".Trim(),
                    Variables = new
                    {
                        filter = new {
                            page = page,
                            sort = "PRICE_DESC",
                            appIds = new ulong[] {
                                UInt64.Parse(appId)
                            },
                            search = (string?) null,
                            cs2ItemCategories = new string[] { },
                            rustItemCategories = new string[] { },
                            itemExteriors = new string[] { },
                            statTrakOnly = false,
                            tradableOnly = false,
                            souvenirOnly = false,
                            minimumPrice = new {
                                coinAmount = 0
                            },
                            maximumPrice = new {
                                coinAmount = 2000000
                            }
                        }
                    }
                };

                var response = await RetryPolicy.ExecuteAsync(() => client.PostAsJsonAsync(url, request));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<RapidSkinsGraphQLResponse<RapidSkinsSiteInventory>>(textJson)?.Data?.SiteInventory?.FirstOrDefault().Value;
                return responseJson;
            }
        }
    }
}
