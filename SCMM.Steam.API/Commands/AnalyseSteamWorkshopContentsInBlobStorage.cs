using Azure.Storage.Blobs;
using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Analytics;
using SCMM.Shared.API;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.API.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Workshop.Models;
using SCMM.Steam.Data.Store;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SCMM.Steam.API.Commands
{
    public class AnalyseSteamWorkshopContentsInBlobStorageRequest : ICommand
    {
        public string BlobName { get; set; }

        public bool Force { get; set; }
    }

    public class AnalyseSteamWorkshopContentsInBlobStorage : ICommandHandler<AnalyseSteamWorkshopContentsInBlobStorageRequest>
    {
        private readonly ILogger<AnalyseSteamWorkshopContentsInBlobStorage> _logger;
        private readonly SteamDbContext _steamDb;
        private readonly IImageAnalysisService _imageAnalysisService;
        private readonly string _workshopFilesStorageConnectionString;

        public AnalyseSteamWorkshopContentsInBlobStorage(ILogger<AnalyseSteamWorkshopContentsInBlobStorage> logger, IConfiguration configuration, SteamDbContext steamDb, IImageAnalysisService imageAnalysisService)
        {
            _logger = logger;
            _steamDb = steamDb;
            _imageAnalysisService = imageAnalysisService;
            _workshopFilesStorageConnectionString = (configuration.GetConnectionString("WorkshopFilesStorageConnection") ?? Environment.GetEnvironmentVariable("WorkshopFilesStorageConnection"));
        }

        public async Task HandleAsync(AnalyseSteamWorkshopContentsInBlobStorageRequest request)
        {
            var hasGlow = (bool?)null;
            var glowRatio = (decimal?)null;
            var hasCutout = (bool?)null;
            var cutoutRatio = (decimal?)null;
            var accentColour = (string)null;
            var dominantColours = (string[])null;
            var tags = new Dictionary<string, string>();

            // Get the workshop file from blob storage
            _logger.LogTrace($"Reading workshop file '{request.BlobName}' from blob storage");
            var blobContainer = new BlobContainerClient(_workshopFilesStorageConnectionString, Constants.BlobContainerWorkshopFiles);
            await blobContainer.CreateIfNotExistsAsync();
            var blob = blobContainer.GetBlobClient(request.BlobName);
            var blobProperties = await blob.GetPropertiesAsync();
            var blobMetadata = blobProperties.Value.Metadata;

            // Inspect the contents of the workshop file
            _logger.LogInformation($"Analysing workshop file '{request.BlobName}' contents");
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
                    var iconAlreadyAnalysed = blobMetadata.ContainsKey(Constants.BlobMetadataIconAnalysed) && !request.Force;
                    if (!iconAlreadyAnalysed)
                    {
                        try
                        {
                            // Determine the icons dominant colour and any captions/tags that help describe the image
                            using var iconStream = iconFile.Open();
                            var iconAnalysis = await _imageAnalysisService.AnalyseImageAsync(iconStream);
                            if (!string.IsNullOrEmpty(iconAnalysis?.AccentColor))
                            {
                                accentColour = $"#{iconAnalysis.AccentColor}";

                                var allColours = new[] { accentColour };
                                var colourTags = new Dictionary<string, string>();
                                foreach (var colour in allColours)
                                {
                                    var colourMatch = ColourName.FindClosestMatch(colour);
                                    if (colourMatch != null)
                                    {
                                        colourTags[colourMatch.Value.Colour] = colourMatch.Value.ColourName;
                                        colourTags[colourMatch.Value.Shade] = colourMatch.Value.ShadeName;
                                    }
                                }
                                tags.AddRange(
                                    colourTags.DistinctBy(x => x.Value).ToDictionary(
                                        x => $"{Constants.AssetTagAiColour}.{x.Key}",
                                        x => x.Value
                                    )
                                );
                            }
                            else
                            {
                                _logger.LogWarning("Icon analyse failed to identify the accent colour");
                            }
                            if (iconAnalysis?.Captions?.Any() == true)
                            {
                                var captionIndex = 0;
                                foreach (var caption in iconAnalysis.Captions)
                                {
                                    var tagName = $"{Constants.AssetTagAiCaption}.{(char)('a' + captionIndex++)}";
                                    tags[tagName] = $"{caption.Key.FirstCharToUpper()} ({Math.Round(caption.Value * 100, 0)}%)";
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Icon analyse failed to identify any captions");
                            }
                            if (iconAnalysis?.Tags?.Any() == true)
                            {
                                var tagIndex = 0;
                                foreach (var tag in iconAnalysis.Tags)
                                {
                                    var tagName = $"{Constants.AssetTagAiTag}.{(char)('a' + tagIndex++)}";
                                    tags[tagName] = tag.FirstCharToUpper();
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Icon analyse failed to identify any tags");
                            }

                            blobMetadata[Constants.BlobMetadataIconAnalysed] = bool.TrueString;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to analyse icon: {iconFile.Name}. {ex.Message}");
                            throw;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Icon analyse was skipped, already completed and force was not specified");
                    }
                }
                else
                {
                    _logger.LogWarning("No icon file present");
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
                            .Where(x => x.Colors.EmissionColor.R > 0 || x.Colors.EmissionColor.G > 0 || x.Colors.EmissionColor.B > 0)
                            .Select(x => workshopFileZip.Entries.FirstOrDefault(f => string.Equals(f.Name, x.Textures.EmissionMap, StringComparison.InvariantCultureIgnoreCase)))
                            .Where(x => x != null)
                            .ToList();
                    }
                }
                else
                {
                    // No manifest present, try manually locate texture files
                    _logger.LogWarning("No manifest file present");
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
                        _logger.LogWarning(ex, $"Failed to detect cutout: {textureFile.Key.Name}. {ex.Message}");
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
                        _logger.LogWarning(ex, $"Failed to detect glow: {emissionMapFile.Name}. {ex.Message}");
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

            _logger.LogTrace($"Analyse results:");
            _logger.LogDebug($"hasGlow = {hasGlow}");
            _logger.LogDebug($"glowRatio = {glowRatio}");
            _logger.LogDebug($"hasCutout = {hasCutout}");
            _logger.LogDebug($"cutoutRatio = {cutoutRatio}");
            _logger.LogDebug($"accentColour = {accentColour}");
            if (dominantColours != null)
            {
                _logger.LogDebug($"dominantColors = {String.Join(", ", dominantColours)}");
            }
            foreach (var tag in tags)
            {
                _logger.LogDebug($"{tag.Key} = {tag.Value}");
            }

            // Update asset descriptions details
            var publishedFileId = ulong.Parse(blobMetadata[Constants.BlobMetadataPublishedFileId]);
            var assetDescriptions = await _steamDb.SteamAssetDescriptions
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
                if (accentColour != null)
                {
                    assetDescription.IconAccentColour = accentColour;
                }
                if (dominantColours != null)
                {
                    assetDescription.IconDominantColours = new PersistableStringCollection(dominantColours);
                }
                if (tags.Any())
                {
                    assetDescription.Tags = new PersistableStringDictionary(
                        assetDescription.Tags
                            .Except(assetDescription.Tags.Where(x => x.Key.StartsWith(Constants.AssetTagAiColour) || x.Key.StartsWith(Constants.AssetTagAiCaption) || x.Key.StartsWith(Constants.AssetTagAiTag)))
                            .ToDictionary(x => x.Key, x => x.Value)
                    );
                    foreach (var tag in tags)
                    {
                        assetDescription.Tags[tag.Key] = tag.Value;
                    }
                }
                _logger.LogInformation($"Asset description workshop metadata updated for '{assetDescription.Name}' ({assetDescription.ClassId})");
            }

            await _steamDb.SaveChangesAsync();

            // Update workshop file metadata
            await blob.SetMetadataAsync(blobMetadata);
            _logger.LogInformation($"Blob metadata updated");
        }
    }
}
