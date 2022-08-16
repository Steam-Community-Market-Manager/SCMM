using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models.Workshop.Requests;
using SCMM.Steam.Data.Models.Workshop.Responses;
using System.Net.Http.Json;
using System.Text.Json;

namespace SCMM.Steam.Client
{
    // NOTE: steamworkshopdownloader.io no longer serves files
    //       https://www.reddit.com/r/swd_io/comments/uy55qg/we_are_no_longer_serving_any_files_through_our/
    
    // TODO: Use http://steamworkshop.download/ instead?

    public class SteamWorkshopDownloaderWebClient : Worker.Client.WebClient
    {
        private readonly ILogger<SteamWorkshopDownloaderWebClient> _logger;

        public SteamWorkshopDownloaderWebClient(ILogger<SteamWorkshopDownloaderWebClient> logger)
        {
            _logger = logger;
        }

        public async Task<WebFileData> DownloadWorkshopFile(SteamWorkshopDownloaderJsonRequest request)
        {
            using (var client = BuildHttpClient())
            {
                try
                {
                    var workshopDownloaderUrl = (string)null;
                    var workshopDownloaderIsOnline = false;
                    for(var workshopDownloaderIndex = 1; workshopDownloaderIndex <= 10; workshopDownloaderIndex++)
                    {
                        try
                        {
                            workshopDownloaderUrl = $"https://node{workshopDownloaderIndex.ToString("00")}.steamworkshopdownloader.io/prod";
                            var steamOfflineResponse = await client.GetAsync($"{workshopDownloaderUrl}/api/download/steamoffline");
                            steamOfflineResponse.EnsureSuccessStatusCode();
                            var steamOffline = await steamOfflineResponse.Content.ReadFromJsonAsync<bool>();
                            if (!steamOffline)
                            {
                                workshopDownloaderIsOnline = true;
                                break;
                            }
                        }
                        catch(Exception ex)
                        {
                            _logger.LogWarning(ex, $"Workshop downloader node #{workshopDownloaderIndex} appears to be offline...");
                        }
                    }
                    if (!workshopDownloaderIsOnline)
                    {
                        throw new Exception("No available workshop downloader nodes are online, unable to download file");
                    }
                    
                    // Request the file
                    request.AutoDownload = true;
                    var downloadResponse = await client.PostAsJsonAsync($"{workshopDownloaderUrl}/api/download/request", request);
                    if (!downloadResponse.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"{downloadResponse.StatusCode}: {downloadResponse.ReasonPhrase}", null, downloadResponse.StatusCode);
                    }

                    var downloadResponseJson = await downloadResponse.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(downloadResponseJson))
                    {
                        return null; // workshop file probably does not exist anymore
                    }

                    var downloadId = JsonSerializer.Deserialize<SteamWorkshopDownloaderJsonResponse>(downloadResponseJson);
                    if (downloadId.Uuid == Guid.Empty)
                    {
                        throw new Exception("No download request UUID was returned, unable to download file");
                    }

                    // Poll until the file is ready
                    var downloadUrl = String.Empty;
                    while (String.IsNullOrEmpty(downloadUrl))
                    {
                        var statusRequest = new SteamWorkshopDownloaderStatusJsonRequest()
                        {
                            Uuids = new Guid[]
                            {
                                downloadId.Uuid
                            }
                        };
                        var statusResponse = await client.PostAsJsonAsync($"{workshopDownloaderUrl}/api/download/status", statusRequest);
                        if (!statusResponse.IsSuccessStatusCode)
                        {
                            throw new HttpRequestException($"{downloadResponse.StatusCode}: {downloadResponse.ReasonPhrase}", null, downloadResponse.StatusCode);
                        }

                        var status = await statusResponse.Content.ReadFromJsonAsync<IDictionary<string, SteamWorkshopDownloaderStatusJsonResponse>>();
                        var downloadStatus = status[downloadId.Uuid.ToString()];
                        switch (downloadStatus?.Status)
                        {
                            case "queued":
                            case "dequeued":
                            case "retrieving":
                            case "retrieved":
                            case "preparing": 
                                Thread.Sleep(3000); 
                                break; // avoid spamming the server
                            case "prepared": 
                            case "transmitted":
                                downloadUrl = $"https://{downloadStatus.StorageNode}/prod/storage/{downloadStatus.StoragePath}?uuid={downloadId.Uuid}";
                                break;
                            default: 
                                throw new Exception($"Unexpected status '{downloadStatus?.Status}' {downloadStatus.DownloadError}");
                        }
                    }

                    // Download the file
                    var storageResponse = await client.GetAsync(downloadUrl);
                    if (!storageResponse.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"{storageResponse.StatusCode}: {storageResponse.ReasonPhrase}", null, downloadResponse.StatusCode);
                    }

                    return new WebFileData()
                    {
                        Name = storageResponse.Content.Headers?.ContentDisposition?.FileName,
                        MimeType = storageResponse.Content.Headers?.ContentType?.MediaType,
                        Data = await storageResponse.Content.ReadAsByteArrayAsync()
                    };
                }
                catch (Exception ex)
                {
                    throw new SteamRequestException($"Download workshop file '{request.PublishedFileId}' failed. {ex.Message}", (ex as HttpRequestException)?.StatusCode, ex);
                }
            }
        }
    }
}
