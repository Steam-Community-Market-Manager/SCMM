using Microsoft.Extensions.Logging;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.WebApi.Models;
using SCMM.Steam.Data.Models.WebApi.Requests.IGameInventory;
using SCMM.Steam.Data.Models.WebApi.Requests.IInventoryService;
using SCMM.Steam.Data.Models.WebApi.Requests.IPublishedFileService;
using SCMM.Steam.Data.Models.WebApi.Responses.IGameInventory;
using SCMM.Steam.Data.Models.WebApi.Responses.IInventoryService;
using SCMM.Steam.Data.Models.WebApi.Responses.IPublishedFileService;
using System.Text.Json;

namespace SCMM.Steam.Client
{
    public class SteamWebApiClient : SteamWebClient
    {
        private readonly SteamConfiguration _configuration;

        public SteamWebApiClient(ILogger<SteamWebApiClient> logger, SteamSession session, SteamConfiguration configuration)
            : base(logger, session)
        {
            _configuration = configuration;
        }

        #region Game Inventory

        public async Task<GetItemDefArchiveJsonResponse> GameInventoryGetItemDefArchive(GetItemDefArchiveJsonRequest request)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJson<GetItemDefArchiveJsonRequest, GetItemDefArchiveJsonResponse>(request);
            return response;
        }

        #endregion

        #region Inventory Service

        public async Task<GetItemDefMetaJsonResponse> InventoryServiceGetItemDefMeta(GetItemDefMetaJsonRequest request)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJson<GetItemDefMetaJsonRequest, SteamResponse<GetItemDefMetaJsonResponse>>(request);
            return response?.Response;
        }

        public async Task<IEnumerable<ItemStackModificationOutcome>> InventoryServiceCombineItemStack(CombineItemStacksJsonRequest request)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJson<CombineItemStacksJsonRequest, SteamResponse< CombineItemStacksJsonResponse>>(request);
            return JsonSerializer.Deserialize<IEnumerable<ItemStackModificationOutcome>>(
                Uri.UnescapeDataString(response?.Response?.ItemJson)
            );
        }

        public async Task<IEnumerable<ItemStackModificationOutcome>> InventoryServiceSplitItemStack(SplitItemStackJsonRequest request)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJson<SplitItemStackJsonRequest, SteamResponse< SplitItemStackJsonResponse>>(request);
            return JsonSerializer.Deserialize<IEnumerable<ItemStackModificationOutcome>>(
                Uri.UnescapeDataString(response?.Response?.ItemJson)
            );
        }

        #endregion

        #region Published File Service

        public async Task<QueryFilesJsonResponse> PublishedFileServiceQueryFiles(QueryFilesJsonRequest request)
        {
            request.Key ??= _configuration?.ApplicationKey;
            var response = await GetJson<QueryFilesJsonRequest, SteamResponse<QueryFilesJsonResponse>>(request);
            return response?.Response;
        }

        #endregion
    }
}
