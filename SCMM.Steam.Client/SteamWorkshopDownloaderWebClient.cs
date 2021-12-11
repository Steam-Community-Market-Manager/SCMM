using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models.Workshop.Requests;
using SCMM.Steam.Data.Models.Workshop.Responses;
using System.Net.Http.Json;
using System.Text.Json;

namespace SCMM.Steam.Client
{
    public class SteamWorkshopDownloaderWebClient
    {
        private readonly ILogger<SteamWorkshopDownloaderWebClient> _logger;
        private readonly HttpClientHandler _httpHandler;
        private readonly string _workshopDownloaderNodeUrl;

        public SteamWorkshopDownloaderWebClient(ILogger<SteamWorkshopDownloaderWebClient> logger, SteamConfiguration configuration)
        {
            _logger = logger;
            _httpHandler = new HttpClientHandler();
            _workshopDownloaderNodeUrl = configuration.WorkshopDownloaderNodeUrl;
        }

        public async Task<WebFileData> DownloadWorkshopFile(SteamWorkshopDownloaderJsonRequest request)
        {
            using (var client = new HttpClient(_httpHandler, false))
            {
                try
                {
                    // Request the file
                    request.AutoDownload = true;
                    var downloadResponse = await client.PostAsJsonAsync($"{_workshopDownloaderNodeUrl}/api/download/request", request);
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
                        var statusResponse = await client.PostAsJsonAsync($"{_workshopDownloaderNodeUrl}/api/download/status", statusRequest);
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
