using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.WebApi.Models;
using SCMM.Steam.Data.Models.WebApi.Requests.IGameInventory;
using SCMM.Steam.Data.Models.WebApi.Requests.IInventoryService;
using SCMM.Steam.Data.Models.WebApi.Requests.IPublishedFileService;
using SCMM.Steam.Data.Models.WebApi.Requests.ISteamEconomy;
using SCMM.Steam.Data.Models.WebApi.Requests.ISteamRemoteStorage;
using SCMM.Steam.Data.Models.WebApi.Requests.ISteamUser;
using SCMM.Steam.Data.Models.WebApi.Responses.IGameInventory;
using SCMM.Steam.Data.Models.WebApi.Responses.IInventoryService;
using SCMM.Steam.Data.Models.WebApi.Responses.IPublishedFileService;
using SCMM.Steam.Data.Models.WebApi.Responses.ISteamEconomy;
using SCMM.Steam.Data.Models.WebApi.Responses.ISteamRemoteStorage;
using SCMM.Steam.Data.Models.WebApi.Responses.ISteamUser;
using System.Text.Json;

namespace SCMM.Steam.Client
{
    /// <summary>
    /// Client for https://api.steampowered.com/
    /// Most requests require an API 'Key' parameter. No session cookie required.
    /// </summary>
    /// <remarks>
    /// Steam web API terms of use (https://steamcommunity.com/dev/apiterms).
    /// You are limited to one hundred thousand (100,000) calls to the Steam Web API per day.
    /// </remarks>
    public class SteamWebApiClient : SteamWebClientBase
    {
        private readonly SteamConfiguration _configuration;

        public SteamWebApiClient(ILogger<SteamWebApiClient> logger, IDistributedCache cache, SteamConfiguration configuration)
            : base(logger, cache)
        {
            _configuration = configuration;
        }

        #region Game Inventory

        public async Task<GetItemDefArchiveJsonResponse> GameInventoryGetItemDefArchiveAsync(GetItemDefArchiveJsonRequest request, bool useCache = false)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJsonAsync<GetItemDefArchiveJsonRequest, GetItemDefArchiveJsonResponse>(request, useCache);
            return response;
        }

        public async Task<string> GameInventoryGetItemDefArchiveRawAsync(GetItemDefArchiveJsonRequest request, bool useCache = false)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetTextAsync(request, useCache);
            return response;
        }

        #endregion

        #region Inventory Service

        public async Task<GetItemDefMetaJsonResponse> InventoryServiceGetItemDefMetaAsync(GetItemDefMetaJsonRequest request, bool useCache = false)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJsonAsync<GetItemDefMetaJsonRequest, SteamResponse<GetItemDefMetaJsonResponse>>(request, useCache);
            return response?.Response;
        }

        public async Task<IEnumerable<ItemStackModificationOutcome>> InventoryServiceCombineItemStackAsync(CombineItemStacksJsonRequest request)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await PostJsonAsync<CombineItemStacksJsonRequest, SteamResponse<CombineItemStacksJsonResponse>>(request);
            return JsonSerializer.Deserialize<IEnumerable<ItemStackModificationOutcome>>(
                Uri.UnescapeDataString(response?.Response?.ItemJson)
            );
        }

        public async Task<IEnumerable<ItemStackModificationOutcome>> InventoryServiceSplitItemStackAsync(SplitItemStackJsonRequest request)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await PostJsonAsync<SplitItemStackJsonRequest, SteamResponse<SplitItemStackJsonResponse>>(request);
            return JsonSerializer.Deserialize<IEnumerable<ItemStackModificationOutcome>>(
                Uri.UnescapeDataString(response?.Response?.ItemJson)
            );
        }

        #endregion

        #region Published File Service

        public async Task<QueryFilesJsonResponse> PublishedFileServiceQueryFilesAsync(QueryFilesJsonRequest request, bool useCache = false)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJsonAsync<QueryFilesJsonRequest, SteamResponse<QueryFilesJsonResponse>>(request, useCache);
            return response?.Response;
        }

        #endregion

        #region Steam Economy

        public async Task<GetAssetClassInfoJsonResponse> SteamEconomyGetAssetClassInfoAsync(GetAssetClassInfoJsonRequest request, bool useCache = false)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJsonAsync<GetAssetClassInfoJsonRequest, SteamResult<GetAssetClassInfoJsonResponse>>(request, useCache);
            return response?.Result;
        }

        public async Task<GetAssetPricesJsonResponse> SteamEconomyGetAssetPricesAsync(GetAssetPricesJsonRequest request, bool useCache = false)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJsonAsync<GetAssetPricesJsonRequest, SteamResult<GetAssetPricesJsonResponse>>(request, useCache);
            return response?.Result;
        }

        #endregion

        #region Steam Remote Storage

        public async Task<GetPublishedFileDetailsJsonResponse> SteamRemoteStorageGetPublishedFileDetailsAsync(GetPublishedFileDetailsJsonRequest request)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await PostJsonAsync<GetPublishedFileDetailsJsonRequest, SteamResponse<GetPublishedFileDetailsJsonResponse>>(request);
            return response?.Response;
        }

        #endregion

        #region Steam User

        public async Task<GetFriendListJsonResponse> SteamUserGetFriendListAsync(GetFriendListJsonRequest request, bool useCache = true)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJsonAsync<GetFriendListJsonRequest, GetFriendListJsonResponse.Result>(request, useCache);
            return response?.FriendsList;
        }

        public async Task<GetPlayerSummariesJsonResponse> SteamUserGetPlayerSummariesAsync(GetPlayerSummariesJsonRequest request, bool useCache = true)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJsonAsync<GetPlayerSummariesJsonRequest, SteamResponse<GetPlayerSummariesJsonResponse>>(request, useCache);
            return response?.Response;
        }

        public async Task<GetPlayerBansJsonResponse> SteamUserGetPlayerBansAsync(GetPlayerBansJsonRequest request, bool useCache = true)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJsonAsync<GetPlayerBansJsonRequest, GetPlayerBansJsonResponse>(request, useCache);
            return response;
        }

        #endregion
    }
}
