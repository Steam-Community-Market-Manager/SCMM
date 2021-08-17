using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.API.Messages;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models.Workshop.Requests;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions
{
    public class DownloadSteamWorkshopFile
    {
        private readonly SteamDbContext _db;
        private readonly SteamWorkshopDownloaderWebClient _workshopDownloaderClient;

        public DownloadSteamWorkshopFile(SteamDbContext db, SteamWorkshopDownloaderWebClient workshopDownloaderClient)
        {
            _db = db;
            _workshopDownloaderClient = workshopDownloaderClient;
        }

        [Function("Download-Steam-Workshop-File")]
        [ServiceBusOutput("steam-workshop-file-analyse", Connection = "ServiceBusConnection")]
        public async Task<AnalyseSteamWorkshopFileMessage> Run([ServiceBusTrigger("steam-workshop-file-downloads", Connection = "ServiceBusConnection")] DownloadSteamWorkshopFileMessage message, FunctionContext context)
        {
            var logger = context.GetLogger("Download-Steam-Workshop-File");

            var blobContainer = new BlobContainerClient(Environment.GetEnvironmentVariable("WorkshopFilesStorage"), "workshop-files");
            await blobContainer.CreateIfNotExistsAsync();

            // If this workshop file is known to be missing, skip over it
            var blobMissingName = $"{message.PublishedFileId}.missing";
            var blobMissing = blobContainer.GetBlobClient(blobMissingName);
            if (blobMissing.Exists()?.Value == true)
            {
                return null;
            }

            // Download the workshop file
            var blobName = $"{message.PublishedFileId}.zip";
            var blob = blobContainer.GetBlobClient(blobName);
            if (blob.Exists()?.Value != true)
            {
                // Download the workshop file from steam
                logger.LogInformation($"Downloading workshop file {message.PublishedFileId} from steam");
                var publishedFileData = await _workshopDownloaderClient.DownloadWorkshopFile(
                    new SteamWorkshopDownloaderJsonRequest()
                    {
                        PublishedFileId = message.PublishedFileId,
                        Extract = false
                    }
                );
                if (publishedFileData?.Data == null)
                {
                    // This file likely has been removed from steam, tag it as missing so we don't try again in the future
                    await blobMissing.UploadAsync(
                        new BinaryData(new byte[0])
                    );
                    await blobMissing.SetMetadataAsync(new Dictionary<string, string>()
                    {
                        { "PublishedFileId", message.PublishedFileId.ToString() }
                    });
                    throw new Exception("Failed to download file, no data, will ignore next time");
                }
                else
                {
                    logger.LogInformation($"Download complete, '{publishedFileData.Name}'");
                }

                // Upload the workshop file to blob storage
                logger.LogInformation($"Uploading workshop file {message.PublishedFileId} to blob storage");
                await blob.UploadAsync(
                    new BinaryData(publishedFileData.Data)
                );
                await blob.SetMetadataAsync(new Dictionary<string, string>()
                {
                    { "PublishedFileId", message.PublishedFileId.ToString() },
                    { "PublishedFileName", publishedFileData.Name }
                });

                logger.LogInformation($"Upload complete, '{blob.Name}'");
            }
            else
            {
                logger.LogWarning($"Download was skipped, blob already exists");
            }

            // Update all asset descriptions that reference this workshop file with the blob url
            var workshopFileUrl = blob.Uri.GetLeftPart(UriPartial.Path);
            var assetDescriptions = await _db.SteamAssetDescriptions
                .Where(x => x.WorkshopFileId == message.PublishedFileId)
                .Where(x => x.WorkshopFileUrl != workshopFileUrl)
                .ToListAsync();
            foreach (var assetDescription in assetDescriptions)
            {
                assetDescription.WorkshopFileUrl = workshopFileUrl;
            }

            await _db.SaveChangesAsync();
            logger.LogInformation($"Asset description workshop data urls updated");

            // Queue analyse of the workfshop file
            return new AnalyseSteamWorkshopFileMessage()
            {
                BlobName = blob.Name
            };
        }
    }
}
