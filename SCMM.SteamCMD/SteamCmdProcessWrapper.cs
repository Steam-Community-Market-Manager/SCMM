using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models;
using SCMM.Steam.Abstractions;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SCMM.SteamCMD;

public class SteamCmdProcessWrapper : ISteamConsoleClient
{
    private readonly ILogger<SteamCmdProcessWrapper> _logger;

    private const string DownloadSteamCmdUrl = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";

    private const string SteamCmdRootPath = "steam";
    private const string SteamCmdExecutableName = "steamcmd.exe";

    public SteamCmdProcessWrapper(ILogger<SteamCmdProcessWrapper> logger)
    {
        _logger = logger;
    }

    private string GetSteamCmdExecutablePath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"{SteamCmdRootPath}{Path.DirectorySeparatorChar}{SteamCmdExecutableName}";
        }

        throw new PlatformNotSupportedException($"SteamCmd does not support this OS platform");
    }

    private async Task<bool> DownloadSteamCmdExecutable()
    {
        // If already exists, exit early
        var steamCmdExecutablePath = GetSteamCmdExecutablePath();
        if (File.Exists(steamCmdExecutablePath))
        {
            return true;
        }

        // If the tools directory doesn't exist, create it now
        if (!Directory.Exists(SteamCmdRootPath))
        {
            Directory.CreateDirectory(SteamCmdRootPath);
        }

        // Download SteamCmd from the internet
        using (var httpClient = new HttpClient())
        {
            var steamCmdZipBytes = await httpClient.GetByteArrayAsync(DownloadSteamCmdUrl);
            if (steamCmdZipBytes?.Length > 0)
            {
                var steamCmdZipPath = $"{steamCmdExecutablePath}.zip";
                File.WriteAllBytes(steamCmdZipPath, steamCmdZipBytes);
                ZipFile.ExtractToDirectory(steamCmdZipPath, SteamCmdRootPath, true);
                File.Delete(steamCmdZipPath);
            }
        }

        return File.Exists(steamCmdExecutablePath);
    }

    public async Task<WebFileData?> DownloadWorkshopFile(string appId, string workshopFileId, bool clearCache = true)
    {
        var workshopFileName = $"{appId}-{workshopFileId}.zip";
        var workshopFileBasePath = $"{SteamCmdRootPath}{Path.DirectorySeparatorChar}steamapps{Path.DirectorySeparatorChar}workshop{Path.DirectorySeparatorChar}content{Path.DirectorySeparatorChar}{appId}{Path.DirectorySeparatorChar}{workshopFileId}";
        var workshopFileZipPath = $"{workshopFileBasePath}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}{workshopFileName}";

        try
        {
            // Get SteamCmd (if not already present)
            var steamCmdExecutablePath = GetSteamCmdExecutablePath();
            if (!File.Exists(steamCmdExecutablePath))
            {
                if (!(await DownloadSteamCmdExecutable()))
                {
                    throw new Exception($"Workshop file download failed, unable to download Steam command line tools");
                }
            }

            // Clear SteamCmd cache directories (if requested)
            if (clearCache)
            {
                // Sometimes Steam will fail with error "Download item {workshopFileId} failed (Failure)".
                // This is normally a temporary error which can be fixed by deleting the cached download and trying again
                var cachedPaths = new string[]
                {
                    $"{SteamCmdRootPath}{Path.DirectorySeparatorChar}appcache",
                    $"{SteamCmdRootPath}{Path.DirectorySeparatorChar}steamapps",
                    $"{SteamCmdRootPath}{Path.DirectorySeparatorChar}userdata"
                };
                foreach (var cachedPath in cachedPaths)
                {
                    if (Path.Exists(cachedPath))
                    {
                        Directory.Delete(cachedPath, true);
                    }
                }
            }

            // Use SteamCmd to download the workshop file
            var downloadProcess = Process.Start(new ProcessStartInfo()
            {
                FileName = steamCmdExecutablePath,
                Arguments = $"+login anonymous +workshop_download_item {appId} {workshopFileId} +quit",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            if (downloadProcess == null)
            {
                throw new Exception($"Workshop file download failed, process could not be started");
            }

            await downloadProcess.WaitForExitAsync();
            if (downloadProcess.ExitCode != 0)
            {
                throw new Exception($"Workshop file download failed, process exited with code {downloadProcess.ExitCode}");
            }

            // If the workshop file did NOT get downloaded, find out why...
            if (!Directory.Exists(workshopFileBasePath))
            {
                var downloadProcessOutput = downloadProcess.StandardOutput.ReadToEnd();
                var errorReason = Regex.Match(downloadProcessOutput, @"ERROR! (.*) \((.*)\)\.").Groups.OfType<Capture>().LastOrDefault()?.Value;
                switch (errorReason)
                {
                    case "Access Denied":
                        throw new UnauthorizedAccessException($"Workshop file download failed, file is private or hidden");
                    case "File Not Found":
                        throw new FileNotFoundException($"Workshop file download failed, file has been deleted");
                    case "Failure":
                        throw new IOException($"Workshop file download failed, generic download failure, try again later");
                    default:
                        throw new Exception($"Workshop file download failed, process completed successfully but no file was downloaded. Error reason was: '{errorReason}'");
                }
            }

            // Package the workshop file in to a zip archive
            ZipFile.CreateFromDirectory(workshopFileBasePath, workshopFileZipPath, CompressionLevel.SmallestSize, includeBaseDirectory: false);
            if (!File.Exists(workshopFileZipPath))
            {
                throw new IOException($"Workshop file download failed, unable to create file content zip archive");
            }

            return new WebFileData()
            {
                Name = workshopFileName,
                MimeType = "application/zip",
                Data = File.ReadAllBytes(workshopFileZipPath)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to download workshop file through SteamCMD (appid: {appId}, workshopfileid: {workshopFileId}). {ex.Message}");
            throw;
        }
        finally
        {
            // Clean up temporary directories
            if (File.Exists(workshopFileZipPath))
            {
                File.Delete(workshopFileZipPath);
            }
            if (Directory.Exists(workshopFileBasePath))
            {
                Directory.Delete(workshopFileBasePath, recursive: true);
            }
        }
    }
}
