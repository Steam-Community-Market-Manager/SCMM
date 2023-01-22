using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.WebApi.Models;
using SCMM.Steam.Data.Models.WebApi.Requests.IGameInventory;
using SCMM.Steam.Data.Models.WebApi.Requests.IInventoryService;
using SCMM.Steam.Data.Models.WebApi.Requests.IPublishedFileService;
using SCMM.Steam.Data.Models.WebApi.Requests.ISteamUser;
using SCMM.Steam.Data.Models.WebApi.Responses.IGameInventory;
using SCMM.Steam.Data.Models.WebApi.Responses.IInventoryService;
using SCMM.Steam.Data.Models.WebApi.Responses.IPublishedFileService;
using SCMM.Steam.Data.Models.WebApi.Responses.ISteamUser;
using System.Text.Json;

namespace SCMM.Steam.Client
{
    /// <summary>
    /// Client for https://api.steampowered.com/.
    /// Most requests require an API 'Key' parameter. No session cookie required.
    /// </summary>
    /// <remarks>
    /// Steam web API terms of use (https://steamcommunity.com/dev/apiterms)
    ///  - You are limited to one hundred thousand (100,000) calls to the Steam Web API per day.
    /// </remarks>
    public class SteamWebApiClient : SteamWebClient
    {
        private readonly SteamConfiguration _configuration;

        public SteamWebApiClient(ILogger<SteamWebApiClient> logger, IDistributedCache cache, SteamConfiguration configuration)
            : base(logger, cache)
        {
            _configuration = configuration;
        }

        #region Game Inventory

        public async Task<GetItemDefArchiveJsonResponse> GameInventoryGetItemDefArchive(GetItemDefArchiveJsonRequest request, bool useCache = false)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJson<GetItemDefArchiveJsonRequest, GetItemDefArchiveJsonResponse>(request, useCache);
            return response;
        }

        public async Task<string> GameInventoryGetItemDefArchiveRaw(GetItemDefArchiveJsonRequest request, bool useCache = false)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetText(request, useCache);
            return response;
        }

        #endregion

        #region Inventory Service

        public async Task<GetItemDefMetaJsonResponse> InventoryServiceGetItemDefMeta(GetItemDefMetaJsonRequest request, bool useCache = false)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJson<GetItemDefMetaJsonRequest, SteamResponse<GetItemDefMetaJsonResponse>>(request, useCache);
            return response?.Response;
        }

        public async Task<IEnumerable<ItemStackModificationOutcome>> InventoryServiceCombineItemStack(CombineItemStacksJsonRequest request)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await PostJson<CombineItemStacksJsonRequest, SteamResponse<CombineItemStacksJsonResponse>>(request);
            return JsonSerializer.Deserialize<IEnumerable<ItemStackModificationOutcome>>(
                Uri.UnescapeDataString(response?.Response?.ItemJson)
            );
        }

        public async Task<IEnumerable<ItemStackModificationOutcome>> InventoryServiceSplitItemStack(SplitItemStackJsonRequest request)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await PostJson<SplitItemStackJsonRequest, SteamResponse<SplitItemStackJsonResponse>>(request);
            return JsonSerializer.Deserialize<IEnumerable<ItemStackModificationOutcome>>(
                Uri.UnescapeDataString(response?.Response?.ItemJson)
            );
        }

        #endregion

        #region Published File Service

        public async Task<QueryFilesJsonResponse> PublishedFileServiceQueryFiles(QueryFilesJsonRequest request, bool useCache = false)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJson<QueryFilesJsonRequest, SteamResponse<QueryFilesJsonResponse>>(request, useCache);
            return response?.Response;
        }

        #endregion

        #region Steam User

        public async Task<GetPlayerSummariesJsonResponse> SteamUserGetPlayerSummaries(GetPlayerSummariesJsonRequest request, bool useCache = true)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJson<GetPlayerSummariesJsonRequest, SteamResponse<GetPlayerSummariesJsonResponse>>(request, useCache);
            return response?.Response;
        }

        public async Task<GetPlayerBansJsonResponse> SteamUserGetPlayerBans(GetPlayerBansJsonRequest request, bool useCache = true)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJson<GetPlayerBansJsonRequest, GetPlayerBansJsonResponse>(request, useCache);
            return response;
        }

        #endregion
    }
}
