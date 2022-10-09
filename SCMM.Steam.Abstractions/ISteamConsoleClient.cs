using SCMM.Shared.Data.Models;

namespace SCMM.Steam.Abstractions;

public interface ISteamConsoleClient
{
    Task<WebFileData> DownloadWorkshopFile(string appId, string workshopFileId);
}
