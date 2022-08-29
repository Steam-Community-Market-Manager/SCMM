using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Messages;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Workshop.Requests;
using SCMM.Steam.Data.Store;
using SCMM.SteamCMD;

namespace SCMM.Steam.Functions.ServiceBus;

public class DownloadWorkshopFileContents
{
    private readonly SteamDbContext _db;
    private readonly SteamCmdWrapper _steamCmd;
    private readonly string _workshopFilesStorageConnectionString;
    private readonly string _workshopFilesStorageUrl;

    private readonly TimeSpan _maxTimeToWaitForAsset = TimeSpan.FromMinutes(1);

    public DownloadWorkshopFileContents(IConfiguration configuration, SteamDbContext db, SteamCmdWrapper steamCmd)
    {
        _db = db;
        _steamCmd = steamCmd;
        _workshopFilesStorageConnectionString = (configuration.GetConnectionString("WorkshopFilesStorageConnection") ?? Environment.GetEnvironmentVariable("WorkshopFilesStorageConnection"));
        _workshopFilesStorageUrl = configuration.GetDataStoreUrl();
    }

    // TODO: Move this to a Windows agent
    //[Function("Download-Workshop-File-Contents")]
    [ServiceBusOutput("analyse-steam-workshop-file", Connection = "ServiceBusConnection")]
    public async Task<AnalyseWorkshopFileContentsMessage> Run([ServiceBusTrigger("download-workshop-file-contents", Connection = "ServiceBusConnection")] DownloadWorkshopFileContentsMessage message, FunctionContext context)
    {
        var logger = context.GetLogger("Download-Workshop-File-Contents");

        var blobContentsWasModified = false;
        var blobContainer = new BlobContainerClient(_workshopFilesStorageConnectionString, Constants.BlobContainerWorkshopFiles);
        await blobContainer.CreateIfNotExistsAsync();

        // If this workshop file is known to be missing, skip over it
        var blobMissingName = $"{message.PublishedFileId}.missing";
        var blobMissing = blobContainer.GetBlobClient(blobMissingName);
        if (blobMissing.Exists()?.Value == true)
        {
            if (message.Force)
            {
                await blobMissing.DeleteAsync();
            }
            else
            {
                return null;
            }
        }

        // Download the workshop file
        var blobName = $"{message.PublishedFileId}.zip";
        var blob = blobContainer.GetBlobClient(blobName);
        if (blob.Exists()?.Value == true && message.Force)
        {
            await blob.DeleteAsync();
        }
        if (blob.Exists()?.Value != true)
        {
            // Download the workshop file from steam
            logger.LogInformation($"Downloading workshop file {message.PublishedFileId} from steam");
            var publishedFileData = await _steamCmd.DownloadWorkshopFile(
                appId: message.AppId.ToString(),
                workshopFileId: message.PublishedFileId.ToString()
            );
            if (publishedFileData?.Data == null)
            {
                // This file likely has been removed from steam, tag it as missing so we don't try again in the future
                await blobMissing.UploadAsync(
                    new BinaryData(new byte[0])
                );
                await blobMissing.SetMetadataAsync(new Dictionary<string, string>()
                {
                    { Constants.BlobMetadataPublishedFileId, message.PublishedFileId.ToString() }
                });
                throw new Exception("Failed to download file, no data, will ignore next time");
            }
            else
            {
                logger.LogTrace($"Download complete, '{publishedFileData.Name}'");
            }

            // Upload the workshop file to blob storage
            logger.LogInformation($"Uploading workshop file {message.PublishedFileId} to blob storage");
            await blob.UploadAsync(
                new BinaryData(publishedFileData.Data)
            );
            await blob.SetMetadataAsync(new Dictionary<string, string>()
            {
                { Constants.BlobMetadataPublishedFileId, message.PublishedFileId.ToString() },
                { Constants.BlobMetadataPublishedFileName, publishedFileData.Name }
            });

            blobContentsWasModified = true;
            logger.LogTrace($"Upload complete, '{blob.Name}'");
        }
        else
        {
            logger.LogWarning($"Download was skipped, blob already exists");
        }

        // Update all asset descriptions that reference this workshop file with the blob url
        var workshopFileUrl = new Uri($"{_workshopFilesStorageUrl}{blob.Uri.AbsolutePath}").ToString();
        var assetDescriptions = new List<SteamAssetDescription>();
        var assetDescriptionCheckStarted = DateTimeOffset.UtcNow;
        do
        {
            // NOTE: It is possible that the asset doesn't yet exist (i.e. it's still transient)
            // TODO: This is a lazy fix, just wait up to 1min for it to be saved before giving up
            Thread.Sleep(TimeSpan.FromSeconds(10));
            assetDescriptions = await _db.SteamAssetDescriptions
                .Where(x => x.WorkshopFileId == message.PublishedFileId)
                .Where(x => x.WorkshopFileUrl != workshopFileUrl)
                .ToListAsync();

        } while (!assetDescriptions.Any() && (DateTimeOffset.UtcNow - assetDescriptionCheckStarted) <= _maxTimeToWaitForAsset);
        foreach (var assetDescription in assetDescriptions)
        {
            assetDescription.WorkshopFileUrl = workshopFileUrl;
        }

        await _db.SaveChangesAsync();
        logger.LogTrace($"Asset description workshop data urls updated (count: {assetDescriptions.Count})");

        // Queue analyse of the workfshop file
        return new AnalyseWorkshopFileContentsMessage()
        {
            BlobName = blob.Name,
            Force = (blobContentsWasModified || message.Force)
        };
    }
}