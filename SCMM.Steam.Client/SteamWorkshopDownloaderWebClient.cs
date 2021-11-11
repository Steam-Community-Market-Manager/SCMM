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

        public SteamWorkshopDownloaderWebClient(ILogger<SteamWorkshopDownloaderWebClient> logger)
        {
            _logger = logger;
            _httpHandler = new HttpClientHandler();
        }

        public async Task<WebFileData> DownloadWorkshopFile(SteamWorkshopDownloaderJsonRequest request)
        {
            using (var client = new HttpClient(_httpHandler, false))
            {
                try
                {
                    // Request the file
                    request.AutoDownload = true;
                    const string downloadUrl = "https://backend-02-prd.steamworkshopdownloader.io/api/download/request";
                    var downloadResponse = await client.PostAsJsonAsync(downloadUrl, request);
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
                    var downloadIsPrepared = false;
                    while (!downloadIsPrepared)
                    {
                        const string statusUrl = $"https://backend-02-prd.steamworkshopdownloader.io/api/download/status";
                        var statusRequest = new SteamWorkshopDownloaderStatusJsonRequest()
                        {
                            Uuids = new Guid[]
                            {
                                downloadId.Uuid
                            }
                        };
                        var statusResponse = await client.PostAsJsonAsync(statusUrl, statusRequest);
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
                            case "preparing": Thread.Sleep(3000); break; // avoid spamming the server
                            case "prepared": downloadIsPrepared = true; break;
                            case "transmitted": downloadIsPrepared = true; break;
                            default: throw new Exception($"Unexpected status '{downloadStatus?.Status}' {downloadStatus.DownloadError}");
                        }
                    }

                    // Download the file
                    var transmitUrl = $"https://backend-02-prd.steamworkshopdownloader.io/api/download/transmit?uuid={downloadId.Uuid}";
                    var transmitResponse = await client.GetAsync(transmitUrl);
                    if (!transmitResponse.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"{transmitResponse.StatusCode}: {transmitResponse.ReasonPhrase}", null, downloadResponse.StatusCode);
                    }

                    return new WebFileData()
                    {
                        Name = transmitResponse.Content.Headers?.ContentDisposition?.FileName,
                        MimeType = transmitResponse.Content.Headers?.ContentType?.MediaType,
                        Data = await transmitResponse.Content.ReadAsByteArrayAsync()
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
