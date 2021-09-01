using Microsoft.Extensions.Logging;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.WebApi.Requests.IPublishedFileService;
using SCMM.Steam.Data.Models.WebApi.Responses.IPublishedFileService;

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

        public async Task<QueryFilesJsonResponse> PublishedFileServiceQueryFiles(QueryFilesJsonRequest request)
        {
            request.Key = _configuration.ApplicationKey;
            var response = await GetJson<QueryFilesJsonRequest, SteamResponse<QueryFilesJsonResponse>>(request);
            return response?.Response;
        }
    }
}
