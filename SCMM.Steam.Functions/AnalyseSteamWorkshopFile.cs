using Azure.Storage.Blobs;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Azure.AI;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.API.Messages;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Workshop.Models;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Functions.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SCMM.Steam.Functions;

public class AnalyseSteamWorkshopFile
{
    private readonly SteamDbContext _db;
    private readonly AzureAiClient _azureAiClient;
    private readonly string _workshopFilesStorageConnectionString;

    public AnalyseSteamWorkshopFile(IConfiguration configuration, SteamDbContext db, AzureAiClient azureAiClient)
    {
        _db = db;
        _azureAiClient = azureAiClient;
        _workshopFilesStorageConnectionString = configuration.GetConnectionString("WorkshopFilesStorageConnection");
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
        var blobContainer = new BlobContainerClient(_workshopFilesStorageConnectionString, Constants.BlobContainerWorkshopFiles);
        await blobContainer.CreateIfNotExistsAsync();
        var blob = blobContainer.GetBlobClient(message.BlobName);
        var blobProperties = await blob.GetPropertiesAsync();
        var blobMetadata = blobProperties.Value.Metadata;

        // Inspect the contents of the workshop file
        logger.LogInformation($"Analysing workshop file contents");
        using var workshopFileDataStream = await blob.OpenReadAsync();
        using (var workshopFileZip = new ZipArchive(workshopFileDataStream, ZipArchiveMode.Read))
        {
            var mainTextureFiles = new Dictionary<ZipArchiveEntry, decimal>();
            var emissionMapFiles = new List<ZipArchiveEntry>();

            // Analyse the icon file (if present)
            var iconFile = workshopFileZip.Entries.FirstOrDefault(
                x => Regex.IsMatch(x.Name, @"(icon[^\.]*|thumb|thumbnail|template|preview)\s*[0-9]*\.(png|jpg|jpeg|jpe|bmp|tga)$", RegexOptions.IgnoreCase)
            );
            if (iconFile != null)
            {
                var iconAlreadyAnalysed = (blobMetadata.ContainsKey(Constants.BlobMetadataIconAnalysed) && !message.Force);
                if (!iconAlreadyAnalysed)
                {
                    try
                    {
                        // Determine the icons dominant colour and any captions/tags that help describe the image
                        using var iconStream = iconFile.Open();
                        var iconAnalysis = await _azureAiClient.AnalyseImageAsync(iconStream, VisualFeatureTypes.Color, VisualFeatureTypes.Description);
                        if (!String.IsNullOrEmpty(iconAnalysis?.Color?.AccentColor))
                        {
                            dominantColour = $"#{iconAnalysis.Color.AccentColor}";
                        }
                        else
                        {
                            logger.LogWarning("Icon analyse failed to identify the dominant colour");
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
                        else
                        {
                            logger.LogWarning("Icon analyse failed to identify any captions");
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
                        else
                        {
                            logger.LogWarning("Icon analyse failed to identify any tags");
                        }

                        blobMetadata[Constants.BlobMetadataIconAnalysed] = Boolean.TrueString;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, $"Failed to analyse icon: {iconFile.Name}. {ex.Message}");
                    }
                }
                else
                {
                    logger.LogWarning("Icon analyse was skipped, already completed and force was not specified");
                }
            }
            else
            {
                logger.LogWarning("No icon file present");
            }

            // Analyse the mainfest file (if present) to locate texture files
            var manifestFile = workshopFileZip.Entries.FirstOrDefault(
                x => string.Equals(x.Name, "manifest.txt", StringComparison.InvariantCultureIgnoreCase)
            );
            if (manifestFile != null)
            {
                using var manifestStream = new StreamReader(manifestFile.Open());
                var manifest = JsonSerializer.Deserialize<SteamWorkshopFileManifest>(manifestStream.ReadToEnd());
                if (manifest != null)
                {
                    mainTextureFiles = manifest.Groups.Where(x => !string.IsNullOrEmpty(x.Textures.MainTex))
                        .ToDictionary(x => workshopFileZip.Entries.FirstOrDefault(f => string.Equals(f.Name, x.Textures.MainTex, StringComparison.InvariantCultureIgnoreCase)), x => x.Floats.Cutoff);
                    emissionMapFiles = manifest.Groups.Where(x => !string.IsNullOrEmpty(x.Textures.EmissionMap))
                        .Where(x => (x.Colors.EmissionColor.R > 0 || x.Colors.EmissionColor.G > 0 || x.Colors.EmissionColor.B > 0))
                        .Select(x => workshopFileZip.Entries.FirstOrDefault(f => string.Equals(f.Name, x.Textures.EmissionMap, StringComparison.InvariantCultureIgnoreCase)))
                        .Where(x => x != null)
                        .ToList();
                }
            }
            else
            {
                // No manifest present, try manually locate texture files
                logger.LogWarning("No manifest file present");
                mainTextureFiles = workshopFileZip.Entries
                    .Where(x => Regex.IsMatch(x.Name, @"(diffuse|albedo|color|colour)\.(png|jpg|jpeg|jpe|bmp|tga)$", RegexOptions.IgnoreCase))
                    .ToDictionary(x => x, x => 1m);
                emissionMapFiles = workshopFileZip.Entries
                    .Where(x => Regex.IsMatch(x.Name, @"(emission)\.(png|jpg|jpeg|jpe|bmp|tga)$", RegexOptions.IgnoreCase))
                    .ToList();
            }

            // Check main textures to see if the item has a cutout (i.e. contain transparent pixels)
            var texturesCutout = new List<decimal>();
            foreach (var textureFile in mainTextureFiles)
            {
                try
                {
                    using var textureStream = textureFile.Key.Open();
                    using var textureImage = await Image.LoadAsync<Rgba32>(textureStream);
                    texturesCutout.Add(
                        textureImage.GetAlphaCuttoffRatio(
                            alphaCutoff: textureFile.Value
                        )
                    );
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, $"Failed to detect cutout: {textureFile.Key.Name}. {ex.Message}");
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

            // Check emission map textures to see if the item glows (i.e. contains non-black pixels)
            var emissionMapsGlow = new List<decimal>();
            foreach (var emissionMapFile in emissionMapFiles)
            {
                try
                {
                    using var emissionMapStream = emissionMapFile.Open();
                    using var emissionMapImage = await Image.LoadAsync<Rgba32>(emissionMapStream);
                    emissionMapsGlow.Add(
                        emissionMapImage.GetEmissionRatio()
                    );
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, $"Failed to detect glow: {emissionMapFile.Name}. {ex.Message}");
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
            if (tags.Any())
            {
                assetDescription.Tags = new PersistableStringDictionary(
                    assetDescription.Tags
                        .Except(assetDescription.Tags.Where(x => x.Key.StartsWith(Constants.AssetTagAiCaption) || x.Key.StartsWith(Constants.AssetTagAiTag)))
                        .ToDictionary(x => x.Key, x => x.Value)
                );
                foreach (var tag in tags)
                {
                    assetDescription.Tags[tag.Key] = tag.Value;
                }
            }
        }

        await _db.SaveChangesAsync();
        logger.LogInformation($"Asset descriptions updated (count: {assetDescriptions.Count})");

        // Update workshop file metadata
        await blob.SetMetadataAsync(blobMetadata);
        logger.LogInformation($"Blob metadata updated");
    }
}
