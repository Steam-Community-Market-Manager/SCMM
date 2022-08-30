using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Azure.ServiceBus;
using SCMM.Shared.API.Messages;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Models.WebApi.Requests.IGameInventory;
using SCMM.Steam.Data.Models.WebApi.Requests.IInventoryService;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.API.Commands
{
    public class ImportSteamItemDefinitionsRequest : ICommand<ImportSteamItemDefinitionsResponse>
    {
        public ulong AppId { get; set; }

        /// <summary>
        /// If null, we'll check for the latest digest before importing items
        /// </summary>
        public string Digest { get; set; }
    }

    public class ImportSteamItemDefinitionsResponse
    {
        public SteamApp App { get; set; }
    }

    public class ImportSteamItemDefinitions : ICommandHandler<ImportSteamItemDefinitionsRequest, ImportSteamItemDefinitionsResponse>
    {
        private readonly ILogger<ImportSteamItemDefinitions> _logger;
        private readonly SteamDbContext _db;
        private readonly SteamWebApiClient _apiClient;
        private readonly ServiceBusClient _serviceBus;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamItemDefinitions(ILogger<ImportSteamItemDefinitions> logger, SteamDbContext db, SteamWebApiClient apiClient, ServiceBusClient serviceBus, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _db = db;
            _apiClient = apiClient;
            _serviceBus = serviceBus;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public async Task<ImportSteamItemDefinitionsResponse> HandleAsync(ImportSteamItemDefinitionsRequest request)
        {
            var app = await _db.SteamApps.FirstOrDefaultAsync(x => x.SteamId == request.AppId.ToString());
            if (app == null)
            {
                throw new Exception($"Unable to find app {request.AppId}");
            }

            // Get the latest item definition digest (if not supplied)
            if (String.IsNullOrEmpty(request.Digest))
            {
                var itemDefMetadata = await _apiClient.InventoryServiceGetItemDefMeta(new GetItemDefMetaJsonRequest()
                {
                    AppId = request.AppId
                });

                var itemDefsDigest = itemDefMetadata?.Digest;
                var itemDefsLastModified = itemDefMetadata?.Modified.SteamTimestampToDateTimeOffset();
                if (itemDefsLastModified != null && (itemDefsLastModified >= app.TimeUpdated || app.TimeUpdated == null) && itemDefsDigest != null && (itemDefMetadata.Digest != app.ItemDefinitionsDigest))
                {
                    app.ItemDefinitionsDigest = itemDefsDigest;
                    app.TimeUpdated = itemDefsLastModified;

                    await _serviceBus.SendMessageAsync(new AppItemDefinitionsUpdatedMessage()
                    {
                        AppId = UInt64.Parse(app.SteamId),
                        AppName = app.Name,
                        AppIconUrl = app.IconUrl,
                        AppColour = app.PrimaryColor,
                        ItemDefinitionsDigest = itemDefsDigest,
                        TimeUpdated = itemDefsLastModified
                    });
                }

                request.Digest = app.ItemDefinitionsDigest;
            }

            if (String.IsNullOrEmpty(request.Digest))
            {
                throw new Exception($"No item definition digest is available for app {request.AppId}");
            }

            // Get the requested item definition archive
            var itemDefinitions = await _apiClient.GameInventoryGetItemDefArchive(new GetItemDefArchiveJsonRequest()
            {
                AppId = request.AppId,
                Digest = request.Digest,
            });

            // Import the item definitions
            var newAssetDescriptions = new List<SteamAssetDescription>();
            var assetDescriptions = await _db.SteamAssetDescriptions.Include(x => x.App).ToListAsync();
            if (itemDefinitions != null)
            {
                foreach (var itemDefinition in itemDefinitions)
                {
                    var assetDescription = assetDescriptions.FirstOrDefault(x =>
                        (x.ItemDefinitionId != null && x.ItemDefinitionId == itemDefinition.ItemDefId) ||
                        (x.WorkshopFileId != null && x.WorkshopFileId == itemDefinition.WorkshopId) ||
                        x.NameHash == itemDefinition.MarketHashName ||
                        x.Name == itemDefinition.MarketName ||
                        x.Name == itemDefinition.Name
                    );
                    if (assetDescription == null)
                    {
                        continue;
                    }

                    await _commandProcessor.ProcessWithResultAsync(new UpdateSteamAssetDescriptionRequest()
                    {
                        AssetDescription = assetDescription,
                        AssetItemDefinition = itemDefinition
                    });
                }
            }

            return new ImportSteamItemDefinitionsResponse
            {
                App = app
            };
        }
    }
}
