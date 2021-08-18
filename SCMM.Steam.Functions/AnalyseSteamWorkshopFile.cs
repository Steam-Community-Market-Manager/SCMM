using Azure.Storage.Blobs;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SCMM.Azure.AI;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.API.Messages;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Workshop.Models;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Functions.Extensions;
using System.Drawing;
using System.IO.Compression;

namespace SCMM.Steam.Functions
{
    public class AnalyseSteamWorkshopFile
    {
        private readonly SteamDbContext _db;
        private readonly AzureAiClient _azureAiClient;

        public AnalyseSteamWorkshopFile(SteamDbContext db, AzureAiClient azureAiClient)
        {
            _db = db;
            _azureAiClient = azureAiClient;
        }

        [Function("Analyse-Steam-Workshop-File")]
        public async Task Run([ServiceBusTrigger("steam-workshop-file-analyse", Connection = "ServiceBusConnection")] AnalyseSteamWorkshopFileMessage message, FunctionContext context)
        {
            var logger = context.GetLogger("Analyse-Steam-Workshop-File");
            var hasGlow = (bool?)null;
            var glowRatio = (decimal?)null;
            var hasCutout = (bool?)null;
            var cutoutRatio = (decimal?)null;
            var dominantColour = (string)null;
            var tags = new Dictionary<string, string>();

            // Get the workshop file from blob storage
            logger.LogInformation($"Reading workshop file '{message.BlobName}' from blob storage");
            var blobContainer = new BlobContainerClient(Environment.GetEnvironmentVariable("WorkshopFilesStorage"), Constants.BlobContainerWorkshopFiles);
            await blobContainer.CreateIfNotExistsAsync();
            var blob = blobContainer.GetBlobClient(message.BlobName);
            var blobProperties = await blob.GetPropertiesAsync();
            var blobMetadata = blobProperties.Value.Metadata;

            // Inspect the contents of the workshop file
            logger.LogInformation($"Analysing workshop file contents");
            using var workshopFileDataStream = await blob.OpenReadAsync();
            using (var workshopFileZip = new ZipArchive(workshopFileDataStream, ZipArchiveMode.Read))
            {
                foreach (var entry in workshopFileZip.Entries)
                {
                    // Inspect the mainfest file
                    if (string.Equals(entry.Name, "manifest.txt", StringComparison.InvariantCultureIgnoreCase))
                    {
                        using var entryStream = new StreamReader(entry.Open());
                        var manifest = JsonConvert.DeserializeObject<SteamWorkshopFileManifest>(entryStream.ReadToEnd());
                        if (manifest != null)
                        {
                            var icon = workshopFileZip.Entries.FirstOrDefault(f => string.Equals(f.Name, "icon.png", StringComparison.InvariantCultureIgnoreCase));
                            var mainTextures = manifest.Groups.Where(x => !string.IsNullOrEmpty(x.Textures.MainTex))
                                .ToDictionary(x => x, x => workshopFileZip.Entries.FirstOrDefault(f => string.Equals(f.Name, x.Textures.MainTex, StringComparison.InvariantCultureIgnoreCase)))
                                .Where(x => x.Value != null);
                            var emissionMaps = manifest.Groups.Where(x => !string.IsNullOrEmpty(x.Textures.EmissionMap))
                                .Where(x => (x.Colors.EmissionColor.R > 0 || x.Colors.EmissionColor.G > 0 || x.Colors.EmissionColor.B > 0))
                                .ToDictionary(x => x, x => workshopFileZip.Entries.FirstOrDefault(f => string.Equals(f.Name, x.Textures.EmissionMap, StringComparison.InvariantCultureIgnoreCase)))
                                .Where(x => x.Value != null);

                            // Check if the item glows (i.e. has an emission map)
                            var emissionMapsGlow = new List<decimal>();
                            foreach (var emissionMap in emissionMaps)
                            {
                                try
                                {
                                    using var emissionMapStream = emissionMap.Value.Open();
                                    using var emissionMapImage = Image.FromStream(emissionMapStream);
                                    emissionMapsGlow.Add(
                                        emissionMapImage.GetEmissionRatio()
                                    );
                                }
                                catch(Exception ex)
                                {
                                    logger.LogWarning(ex, $"Failed to detect glow: {emissionMap.Value?.Name}. {ex.Message}");
                                    continue;
                                }
                            }

                            if (emissionMapsGlow.Any() && emissionMapsGlow.Average() > 0m)
                            {
                                hasGlow = true;
                                glowRatio = emissionMapsGlow.Average();
                            }
                            else
                            {
                                hasGlow = false;
                            }

                            // Check if the item has a cutout (i.e. main textures contain transparency)
                            var texturesCutout = new List<decimal>();
                            foreach (var mainTexture in mainTextures)
                            {
                                try
                                {
                                    using var textureStream = mainTexture.Value.Open();
                                    using var textureImage = Image.FromStream(textureStream);
                                    texturesCutout.Add(
                                        textureImage.GetAlphaCuttoffRatio(
                                            alphaCutoff: mainTexture.Key.Floats.Cutoff
                                        )
                                    );
                                }
                                catch (Exception ex)
                                {
                                    logger.LogWarning(ex, $"Failed to detect cutout: {mainTexture.Value?.Name}. {ex.Message}");
                                    continue;
                                }
                            }

                            if (texturesCutout.Any() && texturesCutout.Average() > 0m)
                            {
                                hasCutout = true;
                                cutoutRatio = texturesCutout.Average();
                            }
                            else
                            {
                                hasCutout = false;
                            }

                            // Analyse the item icon to determine its dominant colour and key words that describe what it looks like
                            var iconAlreadyAnalysed = (blobMetadata.ContainsKey(Constants.BlobMetadataIconAnalysed) && !message.Force);
                            if (icon != null && !iconAlreadyAnalysed)
                            {
                                try
                                {
                                    using var iconStream = icon.Open();
                                    var iconAnalysis = await _azureAiClient.AnalyseImageAsync(iconStream, VisualFeatureTypes.Color, VisualFeatureTypes.Description);
                                    if (!String.IsNullOrEmpty(iconAnalysis?.Color?.AccentColor))
                                    {
                                        dominantColour = $"#{iconAnalysis.Color.AccentColor}";
                                    }
                                    if (iconAnalysis?.Description?.Captions?.Any() == true)
                                    {
                                        var captionIndex = 0;
                                        foreach (var caption in iconAnalysis.Description.Captions)
                                        {
                                            var tagName = $"{Constants.AssetTagAiCaption}.{(char)('a' + captionIndex++)}";
                                            tags[tagName] = $"{caption.Text.FirstCharToUpper()} ({Math.Round(caption.Confidence * 100, 0)}%)";
                                        }
                                    }
                                    if (iconAnalysis?.Description?.Tags?.Any() == true)
                                    {
                                        var tagIndex = 0;
                                        foreach (var tag in iconAnalysis.Description.Tags)
                                        {
                                            var tagName = $"{Constants.AssetTagAiTag}.{(char)('a' + tagIndex++)}";
                                            tags[tagName] = tag.FirstCharToUpper();
                                        }
                                    }

                                    blobMetadata[Constants.BlobMetadataIconAnalysed] = Boolean.TrueString;
                                }
                                catch (Exception ex)
                                {
                                    logger.LogWarning(ex, $"Failed to analyse icon: {icon.Name}. {ex.Message}");
                                    continue;
                                }
                            }
                        }
                    }
                }
            }

            logger.LogInformation($"Analyse complete");
            logger.LogInformation($"hasGlow = {hasGlow}");
            logger.LogInformation($"glowRatio = {glowRatio}");
            logger.LogInformation($"hasCutout = {hasCutout}");
            logger.LogInformation($"cutoutRatio = {cutoutRatio}");
            logger.LogInformation($"dominantColour = {dominantColour}");
            foreach (var tag in tags)
            {
                logger.LogInformation($"{tag.Key} = {tag.Value}");
            }

            // Update asset descriptions details
            var publishedFileId = ulong.Parse(blobMetadata[Constants.BlobMetadataPublishedFileId]);
            var assetDescriptions = await _db.SteamAssetDescriptions
                .Where(x => x.WorkshopFileId == publishedFileId)
                .ToListAsync();

            foreach (var assetDescription in assetDescriptions)
            {
                if (hasGlow != null)
                {
                    assetDescription.HasGlow = hasGlow;
                }
                if (glowRatio != null)
                {
                    assetDescription.GlowRatio = glowRatio;
                }
                if (hasCutout != null)
                {
                    assetDescription.HasCutout = hasCutout;
                }
                if (cutoutRatio != null)
                {
                    assetDescription.CutoutRatio = cutoutRatio;
                }
                if (dominantColour != null)
                {
                    assetDescription.DominantColour = dominantColour;
                }
                if (assetDescription.Tags.Serialised.Contains("glowsight=true", StringComparison.InvariantCultureIgnoreCase))
                {
                    assetDescription.HasGlowSights = true;
                }
                assetDescription.Tags = new PersistableStringDictionary(
                    assetDescription.Tags
                        .Except(assetDescription.Tags.Where(x => x.Key.StartsWith(Constants.AssetTagAiCaption) || x.Key.StartsWith(Constants.AssetTagAiTag) || x.Key.StartsWith("glow") || x.Key.StartsWith("glowsight") || x.Key.StartsWith("cutout")))
                        .ToDictionary(x => x.Key, x => x.Value)
                );
                if (tags.Any())
                {
                    foreach (var tag in tags)
                    {
                        assetDescription.Tags[tag.Key] = tag.Value;
                    }
                }
            }

            await _db.SaveChangesAsync();
            logger.LogInformation($"Asset descriptions updated");

            // Update workshop file metadata
            await blob.SetMetadataAsync(blobMetadata);
            logger.LogInformation($"Blob metadata updated");
        }
    }
}
