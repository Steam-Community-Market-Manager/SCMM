using Azure.Storage.Blobs;
using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.API.Messages;
using SCMM.Shared.Data.Models;
using SCMM.Steam.Abstractions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.API.Commands
{
    public class ImportSteamWorkshopFileToBlobStorageRequest : ICommand<ImportSteamWorkshopFileToBlobStorageResponse>
    {
        public ulong AppId { get; set; }

        public ulong PublishedFileId { get; set; }

        public bool Force { get; set; }
    }

    public class ImportSteamWorkshopFileToBlobStorageResponse
    {
        public string FileUrl { get; set; }
    }

    public class ImportSteamWorkshopFileToBlobStorage : ICommandHandler<ImportSteamWorkshopFileToBlobStorageRequest, ImportSteamWorkshopFileToBlobStorageResponse>
    {

        private readonly ILogger<ImportSteamWorkshopFileToBlobStorage> _logger;
        private readonly SteamDbContext _steamDb;
        private readonly ISteamConsoleClient _steamConsoleClient;
        private readonly IServiceBus _serviceBus;
        private readonly string _workshopFilesStorageConnectionString;
        private readonly string _workshopFilesStorageUrl;

        public ImportSteamWorkshopFileToBlobStorage(ILogger<ImportSteamWorkshopFileToBlobStorage> logger, IConfiguration configuration, SteamDbContext steamDb, ISteamConsoleClient steamConsoleClient, IServiceBus serviceBus)
        {
            _logger = logger;
            _steamDb = steamDb;
            _steamConsoleClient = steamConsoleClient;
            _serviceBus = serviceBus;
            _workshopFilesStorageConnectionString = (configuration.GetConnectionString("WorkshopFilesStorageConnection") ?? Environment.GetEnvironmentVariable("WorkshopFilesStorageConnection"));
            _workshopFilesStorageUrl = configuration.GetDataStoreUrl();
        }

        public async Task<ImportSteamWorkshopFileToBlobStorageResponse> HandleAsync(ImportSteamWorkshopFileToBlobStorageRequest request)
        {
            var blobContentsWasModified = false;
            var blobContainer = new BlobContainerClient(_workshopFilesStorageConnectionString, Constants.BlobContainerWorkshopFiles);
            await blobContainer.CreateIfNotExistsAsync();

            // If this workshop file is known to be missing, skip over it
            var blobMissingName = $"{request.PublishedFileId}.missing";
            var blobMissing = blobContainer.GetBlobClient(blobMissingName);
            if (blobMissing.Exists()?.Value == true)
            {
                if (request.Force)
                {
                    await blobMissing.DeleteAsync();
                }
                else
                {
                    _logger.LogWarning($"Download was skipped, workshop is known to be unavailable");
                    await UpdateAssetDescriptionsWorkshopFileAsPermanentlyUnavilable(request.PublishedFileId);
                    return null;
                }
            }

            // Download the workshop file
            var blobName = $"{request.PublishedFileId}.zip";
            var blob = blobContainer.GetBlobClient(blobName);
            if (blob.Exists()?.Value == true && request.Force)
            {
                await blob.DeleteAsync();
            }
            if (blob.Exists()?.Value != true)
            {
                var publishedFileData = (WebFileData)null;

                try
                {
                    // Download the workshop file from Steam
                    _logger.LogTrace($"Downloading workshop file {request.PublishedFileId} from steam");
                    publishedFileData = await _steamConsoleClient.DownloadWorkshopFile(
                        appId: request.AppId.ToString(),
                        workshopFileId: request.PublishedFileId.ToString(),
                        clearCache: true
                    );
                    if (publishedFileData?.Data != null)
                    {
                        _logger.LogTrace($"Download of {request.PublishedFileId} complete, '{publishedFileData.Name}'");
                    }
                    else
                    {
                        throw new NullReferenceException("Workshop file data was expected but is null");
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    // This file is permanently unavailable, tag it as missing so we don't try again in the future
                    _logger.LogWarning(ex, $"Workshop file {request.PublishedFileId} is permanently unavailable, will ignore next time");
                    await blobMissing.UploadAsync(
                        new BinaryData(new byte[0])
                    );
                    await blobMissing.SetMetadataAsync(new Dictionary<string, string>()
                    {
                        { Constants.BlobMetadataPublishedFileId, request.PublishedFileId.ToString() }
                    });

                    await UpdateAssetDescriptionsWorkshopFileAsPermanentlyUnavilable(request.PublishedFileId);
                    return null;
                }
                catch (FileNotFoundException ex)
                {
                    // This file is permanently unavailable, tag it as missing so we don't try again in the future
                    _logger.LogWarning(ex, $"Workshop file {request.PublishedFileId} is permanently unavailable, will ignore next time");
                    await blobMissing.UploadAsync(
                        new BinaryData(new byte[0])
                    );
                    await blobMissing.SetMetadataAsync(new Dictionary<string, string>()
                    {
                        { Constants.BlobMetadataPublishedFileId, request.PublishedFileId.ToString() }
                    });

                    await UpdateAssetDescriptionsWorkshopFileAsPermanentlyUnavilable(request.PublishedFileId);
                    return null;
                }
                catch (IOException ex)
                {
                    // This file is temporarily unavailable, either it failed to download or failed to read it. Try again later
                    _logger.LogWarning(ex, $"Workshop file {request.PublishedFileId} is temporarily unavailable, try again later");
                    throw;
                }

                // Upload the workshop file to blob storage
                _logger.LogTrace($"Uploading workshop file {request.PublishedFileId} to blob storage");
                await blob.UploadAsync(
                    new BinaryData(publishedFileData.Data)
                );
                await blob.SetMetadataAsync(new Dictionary<string, string>()
                {
                    { Constants.BlobMetadataPublishedFileId, request.PublishedFileId.ToString() },
                    { Constants.BlobMetadataPublishedFileName, publishedFileData.Name }
                });

                blobContentsWasModified = true;
                _logger.LogTrace($"Upload of {request.PublishedFileId} complete, '{blob.Name}'");
            }
            else
            {
                _logger.LogWarning($"Download of {request.PublishedFileId} was skipped, blob already exists");
            }

            // Update all asset descriptions that reference this workshop file with the blob url
            await UpdateAssetDescriptionsWorkshopFileAsAvilable(
                request.PublishedFileId,
                new Uri($"{_workshopFilesStorageUrl}{blob.Uri.AbsolutePath}").ToString()
            );

            // Queue analyse of the workfshop file
            await _serviceBus.SendMessageAsync(new AnalyseWorkshopFileContentsMessage()
            {
                BlobName = blob.Name,
                Force = (blobContentsWasModified || request.Force)
            });

            return new ImportSteamWorkshopFileToBlobStorageResponse()
            {
                FileUrl = blob.Uri.ToString()
            };
        }

        private async Task UpdateAssetDescriptionsWorkshopFileAsAvilable(ulong publishedFileId, string workshopFileUrl)
        {
            var assetDescriptions = new List<SteamAssetDescription>();
            var assetDescriptionCheckStarted = DateTimeOffset.UtcNow;
            var maxTimeToWaitForAsset = TimeSpan.FromMinutes(1);

            do
            {
                // NOTE: It is possible that the asset doesn't yet exist (i.e. it's still transient)
                // TODO: This is a lazy fix, just wait up to 1min for it to be saved before giving up
                assetDescriptions = await _steamDb.SteamAssetDescriptions
                    .Where(x => x.WorkshopFileId == publishedFileId)
                    .ToListAsync();
                if (!assetDescriptions.Any())
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }
            } while (!assetDescriptions.Any() && (DateTimeOffset.UtcNow - assetDescriptionCheckStarted) <= maxTimeToWaitForAsset);

            foreach (var assetDescription in assetDescriptions)
            {
                _logger.LogTrace($"Asset description workshop data url updated for '{assetDescription.Name}' ({assetDescription.ClassId})");
                assetDescription.WorkshopFileUrl = workshopFileUrl;
                assetDescription.WorkshopFileIsUnavailable = false;
            }

            await _steamDb.SaveChangesAsync();
        }

        private async Task UpdateAssetDescriptionsWorkshopFileAsPermanentlyUnavilable(ulong publishedFileId)
        {
            var assetDescriptions = await _steamDb.SteamAssetDescriptions
                    .Where(x => x.WorkshopFileId == publishedFileId)
                    .Where(x => String.IsNullOrEmpty(x.WorkshopFileUrl) && !x.WorkshopFileIsUnavailable)
                    .ToListAsync();

            foreach (var assetDescription in assetDescriptions)
            {
                _logger.LogTrace($"Asset description workshop data is permanently unavailable for '{assetDescription.Name}' ({assetDescription.ClassId})");
                assetDescription.WorkshopFileIsUnavailable = true;
            }

            await _steamDb.SaveChangesAsync();
        }
    }
}
