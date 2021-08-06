using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.API.Extensions;
using SCMM.Steam.API.Messages;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Workshop.Models;
using SCMM.Steam.Data.Store;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Steam.Functions
{
    public class AnalyseSteamWorkshopFile
    {
        private readonly SteamDbContext _db;

        public AnalyseSteamWorkshopFile(SteamDbContext db)
        {
            _db = db;
        }

        [Function("Analyse-Steam-Workshop-File")]
        public async Task Run([ServiceBusTrigger("steam-workshop-file-analyse", Connection = "ServiceBusConnection")] AnalyseSteamWorkshopFileMessage message, FunctionContext context)
        {
            var logger = context.GetLogger("Analyse-Steam-Workshop-File");

            // Get the workshop file from blob storage
            logger.LogInformation($"Reading workshop file '{message.BlobName}' from blob storage");
            var blobContainer = new BlobContainerClient(Environment.GetEnvironmentVariable("WorkshopFilesStorage"), "workshop-files");
            await blobContainer.CreateIfNotExistsAsync();
            var blob = blobContainer.GetBlobClient(message.BlobName);
            var blobProperties = await blob.GetPropertiesAsync();
            var blobMetadata = blobProperties.Value.Metadata;

            // Inspect the contents of the workshop file
            logger.LogInformation($"Analysing workshop file contents");
            var publishedFileId = ulong.Parse(blobMetadata["PublishedFileId"]);
            var publishedFileTags = new Dictionary<string, string>();
            using var workshopFileDataStream = await blob.OpenReadAsync();
            using (var workshopFileZip = new ZipArchive(workshopFileDataStream, ZipArchiveMode.Read))
            {
                foreach (var entry in workshopFileZip.Entries)
                {
                    // Inspect the mainfest file
                    if (String.Equals(entry.Name, "manifest.txt", StringComparison.InvariantCultureIgnoreCase))
                    {
                        using var entryStream = new StreamReader(entry.Open());
                        var manifest = JsonConvert.DeserializeObject<SteamWorkshopFileManifest>(entryStream.ReadToEnd());
                        if (manifest != null)
                        {
                            // Check if the item glows (i.e. has an emission map)
                            var emissionMaps = manifest.Groups.Where(x => !String.IsNullOrEmpty(x.Textures.EmissionMap))
                                .Where(x => (x.Colors.EmissionColor.R > 0 || x.Colors.EmissionColor.G > 0 || x.Colors.EmissionColor.B > 0))
                                .Select(x => workshopFileZip.Entries.FirstOrDefault(f => String.Equals(f.Name, x.Textures.EmissionMap, StringComparison.InvariantCultureIgnoreCase)));

                            var emissionMapsGlow = new List<decimal>();
                            foreach (var emissionMap in emissionMaps)
                            {
                                using var emissionMapStream = emissionMap.Open();
                                using var emissionMapImage = Image.FromStream(emissionMapStream);
                                emissionMapsGlow.Add(
                                    emissionMapImage.GetEmissionRatio()
                                );
                            }

                            if (emissionMapsGlow.Any() && emissionMapsGlow.Average() > 0m)
                            {
                                publishedFileTags.SetFlag(Constants.RustAssetTagGlow, emissionMapsGlow.Average());
                            }
                            else
                            {
                                publishedFileTags.SetFlag(Constants.RustAssetTagGlow, false);
                            }

                            // Check if the item has a cutout (i.e. main textures contain transparency)
                            var textures = manifest.Groups.Where(x => !String.IsNullOrEmpty(x.Textures.MainTex))
                                .Select(x => workshopFileZip.Entries.FirstOrDefault(f => String.Equals(f.Name, x.Textures.MainTex, StringComparison.InvariantCultureIgnoreCase)));

                            var texturesCutout = new List<decimal>();
                            foreach (var texture in textures)
                            {
                                using var textureStream = texture.Open();
                                using var textureImage = Image.FromStream(textureStream);
                                texturesCutout.Add(
                                    textureImage.GetTransparencyRatio(
                                        alphaCutoff: 128 // pixel must be at least 50% transparent to count
                                    )
                                );
                            }

                            if (texturesCutout.Any() && texturesCutout.Average() > 0m)
                            {
                                publishedFileTags.SetFlag(Constants.RustAssetTagCutout, texturesCutout.Average());
                            }
                            else
                            {
                                publishedFileTags.SetFlag(Constants.RustAssetTagCutout, false);
                            }
                        }
                    }
                }
            }

            logger.LogInformation(String.Join("\n", publishedFileTags.Select(x => $"{x.Key} = {x.Value}")));
            logger.LogInformation($"Analyse complete");

            // Update workshop file metadata
            var newBlobMetadata = publishedFileTags.Union(blobMetadata).ToDictionary(x => x.Key, x => x.Value);
            await blob.SetMetadataAsync(newBlobMetadata);
            logger.LogInformation($"Blob metadata updated");

            // Update asset descriptions tags
            var assetDescriptions = await _db.SteamAssetDescriptions
                .Where(x => x.WorkshopFileId == publishedFileId)
                .ToListAsync();
            foreach (var assetDescription in assetDescriptions)
            {
                assetDescription.Tags = new PersistableStringDictionary(assetDescription.Tags);
                foreach (var tag in publishedFileTags)
                {
                    assetDescription.Tags[tag.Key] = tag.Value;
                }
            }

            await _db.SaveChangesAsync();
            logger.LogInformation($"Asset description tags updated");
        }
    }
}
