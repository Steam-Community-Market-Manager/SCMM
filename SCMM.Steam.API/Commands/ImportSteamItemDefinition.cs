using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.API.Messages;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models.WebApi.Models;
using SCMM.Steam.Data.Store;
namespace SCMM.Steam.API.Commands
{
    public class ImportSteamItemDefinitionRequest : ICommand<ImportSteamItemDefinitionResponse>
    {
        public ulong AppId { get; set; }

        public ulong ItemDefinitionId { get; set; }

        public string ItemDefinitionName { get; set; }

        /// <summary>
        /// Optional, removes the need to lookup AssetClassId if supplied
        /// </summary>
        public ItemDefinition ItemDefinition { get; set; }
    }

    public class ImportSteamItemDefinitionResponse
    {
        /// <remarks>
        /// If asset does not exist, this will be null
        /// </remarks>
        public SteamAssetDescription AssetDescription { get; set; }
    }

    public class ImportSteamItemDefinition : ICommandHandler<ImportSteamItemDefinitionRequest, ImportSteamItemDefinitionResponse>
    {
        private readonly ILogger<ImportSteamItemDefinition> _logger;
        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly SteamWebApiClient _apiClient;
        private readonly IServiceBus _serviceBus;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamItemDefinition(ILogger<ImportSteamItemDefinition> logger, SteamDbContext db, IConfiguration cfg, SteamWebApiClient apiClient, IServiceBus serviceBus, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _apiClient = apiClient;
            _serviceBus = serviceBus;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public async Task<ImportSteamItemDefinitionResponse> HandleAsync(ImportSteamItemDefinitionRequest request)
        {
            // Does this asset already exist?
            var assetDescription = (await _db.SteamAssetDescriptions.Include(x => x.App).FirstOrDefaultAsync(x => x.ItemDefinitionId == request.ItemDefinitionId)) ??
                                   (_db.SteamAssetDescriptions.Local.FirstOrDefault(x => x.ItemDefinitionId == request.ItemDefinitionId));
            if (assetDescription == null)
            {
                // Doesn't exist in database, create it now...
                _db.SteamAssetDescriptions.Add(assetDescription = new SteamAssetDescription()
                {
                    App = await _db.SteamApps.FirstOrDefaultAsync(x => x.SteamId == request.AppId.ToString()),
                    ItemDefinitionId = request.ItemDefinitionId
                });
            }

            // Get item definition info
            var itemDefinition = request.ItemDefinition;
            if (request.ItemDefinition == null)
            {
                // TODO: We need to fetch it from Steam...
            }
            if (itemDefinition == null)
            {
                throw new Exception($"Failed to get definition info for item {request.ItemDefinition}, item was not found");
            }

            // Update the asset description
            var updateAssetDescription = await _commandProcessor.ProcessWithResultAsync(new UpdateSteamAssetDescriptionRequest()
            {
                AssetDescription = assetDescription,
                AssetItemDefinition = itemDefinition
            });

            // If the asset description is now persistent (not transient)...
            if (!assetDescription.IsTransient)
            {
                // Queue a download of the workshop file data for analyse (if it's missing or has changed since our last check)
                if (assetDescription.WorkshopFileId > 0 && string.IsNullOrEmpty(assetDescription.WorkshopFileUrl) && !assetDescription.WorkshopFileIsUnavailable)
                {
                    var app = await _db.SteamApps.FirstOrDefaultAsync(x => x.SteamId == request.AppId.ToString());
                    if (app?.IsActive == true)
                    {
                        await _serviceBus.SendMessageAsync(new ImportWorkshopFileContentsMessage()
                        {
                            AppId = request.AppId,
                            PublishedFileId = assetDescription.WorkshopFileId.Value,
                            Force = false
                        });
                    }
                }
            }

            return new ImportSteamItemDefinitionResponse
            {
                AssetDescription = assetDescription
            };
        }
    }
}
