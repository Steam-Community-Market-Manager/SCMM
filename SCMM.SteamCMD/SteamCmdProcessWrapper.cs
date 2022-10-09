using Microsoft.Extensions.Logging;
using SCMM.Steam.Abstractions;
using SCMM.Shared.Data.Models;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace SCMM.SteamCMD;
public class SteamCmdProcessWrapper : ISteamConsoleClient
{
    private readonly ILogger<SteamCmdProcessWrapper> _logger;

    public SteamCmdProcessWrapper(ILogger<SteamCmdProcessWrapper> logger)
    {
        _logger = logger;
    }

    public async Task<WebFileData> DownloadWorkshopFile(string appId, string workshopFileId)
    {
        var workshopFileName = $"{appId}-{workshopFileId}.zip";
        var workshopFileBasePath = $"Tools{Path.DirectorySeparatorChar}steamapps{Path.DirectorySeparatorChar}workshop{Path.DirectorySeparatorChar}content{Path.DirectorySeparatorChar}{appId}{Path.DirectorySeparatorChar}{workshopFileId}";
        var workshopFileZipPath = $"{workshopFileBasePath}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}{workshopFileName}";

        try
        {
            var steamCmdPath = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                steamCmdPath = $"Tools{Path.DirectorySeparatorChar}steamcmd.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                steamCmdPath = $"Tools{Path.DirectorySeparatorChar}steamcmd.sh";
                // TODO: $"chmod 755 {steamCmdPath}"
            }
            if (String.IsNullOrEmpty(steamCmdPath))
            {
                throw new PlatformNotSupportedException($"Workshop file download failed, OS platform is not supported");
            }

            var downloadProcess = Process.Start(new ProcessStartInfo()
            {
                FileName = steamCmdPath,
                Arguments = $"+login anonymous +workshop_download_item {appId} {workshopFileId} +quit",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
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

            if (!Directory.Exists(workshopFileBasePath))
            {
                _logger.LogWarning("Workshop file cannot be found, it either has been deleted or is private");
                return null;
            }

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
