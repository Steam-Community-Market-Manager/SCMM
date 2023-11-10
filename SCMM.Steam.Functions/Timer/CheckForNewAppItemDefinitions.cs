using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.API.Events;
using SCMM.Shared.API.Messages;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Models.WebApi.Requests.IInventoryService;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class CheckForNewAppItemDefinitions
{
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly SteamDbContext _steamDb;
    private readonly IServiceBus _serviceBus;
    private readonly SteamWebApiClient _apiClient;

    public CheckForNewAppItemDefinitions(ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, SteamDbContext steamDb, SteamWebApiClient apiClient, IServiceBus serviceBus)
    {
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
        _steamDb = steamDb;
        _apiClient = apiClient;
        _serviceBus = serviceBus;
    }

    [Function("Check-New-App-Item-Definitions")]
    public async Task Run([TimerTrigger("0/30 * * * * *")] /* every 30 seconds */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-App-Item-Definitions");

        var apps = await _steamDb.SteamApps
            .Where(x => x.IsActive)
            .ToArrayAsync();

        foreach (var app in apps)
        {
            logger.LogTrace($"Checking for new item definition archives (appId: {app.SteamId})");

            // Get the latest item definition digest info
            var itemDefMetadata = await _apiClient.InventoryServiceGetItemDefMetaAsync(new GetItemDefMetaJsonRequest()
            {
                AppId = UInt64.Parse(app.SteamId)
            });

            // Has the digest actually changed?
            var itemDefsDigest = itemDefMetadata?.Digest;
            if (!String.IsNullOrEmpty(itemDefsDigest) && !String.Equals(itemDefsDigest, app.ItemDefinitionsDigest, StringComparison.OrdinalIgnoreCase))
            {
                var itemDefsLastModified = itemDefMetadata?.Modified.SteamTimestampToDateTimeOffset();
                if (itemDefsLastModified != null && (itemDefsLastModified >= app.TimeUpdated || app.TimeUpdated == null))
                {
                    logger.LogInformation($"Found new item definition archive (appId: {app.SteamId}, digest: {itemDefsDigest})");
                    await _serviceBus.SendMessageAsync(new AppItemDefinitionsUpdatedMessage()
                    {
                        AppId = UInt64.Parse(app.SteamId),
                        AppName = app.Name,
                        AppIconUrl = app.IconUrl,
                        AppColour = app.PrimaryColor,
                        ItemDefinitionsDigest = itemDefsDigest,
                        TimeUpdated = itemDefsLastModified
                    });

                    // Update the app with the latest item definitions digest info
                    app.ItemDefinitionsDigest = itemDefsDigest;
                    app.TimeUpdated = itemDefsLastModified;
                    await _steamDb.SaveChangesAsync();

                    // Import the item definitions archive and parse any item changes
                    await _serviceBus.SendMessageAsync(new ImportAppItemDefinitionsArchiveMessage()
                    {
                        AppId = app.SteamId,
                        ItemDefinitionsDigest = itemDefsDigest,
                        ParseChanges = true
                    });
                }
            }
        }
    }
}
