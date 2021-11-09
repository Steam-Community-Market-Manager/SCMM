using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Google.Client;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Data.Store;
using System.Text.RegularExpressions;

namespace SCMM.Steam.Job.Server.Jobs;

public class CheckNewYouTubeStoreVideosConfiguration
{
    public ChannelExpression[] Channels { get; set; }

    public class ChannelExpression
    {
        public string ChannelId { get; set; }

        public string Query { get; set; }
    }
}

public class CheckNewYouTubeStoreVideos
{
    private readonly SteamDbContext _db;
    private readonly GoogleClient _googleClient;
    private readonly CheckNewYouTubeStoreVideosConfiguration _configuration;

    public CheckNewYouTubeStoreVideos(IConfiguration configuration, SteamDbContext db, GoogleClient googleClient)
    {
        _db = _db;
        _googleClient = _googleClient;
        _configuration = configuration.GetSection("StoreVideos").Get<CheckNewYouTubeStoreVideosConfiguration>();
    }

    [Function("Check-New-YouTube-Store-Videos")]
    public async Task Run([TimerTrigger("0 0 * * * *")] /* every hour */ object timer, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-YouTube-Store-Videos");

        var steamApps = await _db.SteamApps.ToListAsync();
        if (!steamApps.Any())
        {
            return;
        }

        foreach (var app in steamApps)
        {
            var activeItemStores = await _db.SteamItemStores
                .Where(x => x.End == null)
                .OrderByDescending(x => x.Start)
                .ToListAsync();

            foreach (var itemStore in activeItemStores)
            {
                var media = new Dictionary<DateTimeOffset, string>();
                foreach (var channel in _configuration.Channels)
                {
                    try
                    {
                        // TODO: If we already have a video for this channel, skip it
                        // NOTE: We only accept one video per-channel, per-store
                        /*
                        if (itemStore.Media.ContainsKey(channel.ChannelId))
                        {
                            continue;
                        }
                        */

                        // Find the earliest video that matches our store data period.
                        logger.LogInformation($"Checking channel (id: {channel.ChannelId}) for new store videos since {itemStore.Start.UtcDateTime}...");
                        var videos = await _googleClient.ListChannelVideosAsync(channel.ChannelId, GoogleClient.PageMaxResults);
                        var firstStoreVideo = videos
                            .Where(x => Regex.IsMatch(x.Title, channel.Query, RegexOptions.IgnoreCase))
                            .Where(x => x.PublishedAt != null && x.PublishedAt.Value.UtcDateTime >= itemStore.Start.UtcDateTime && x.PublishedAt.Value.UtcDateTime <= itemStore.Start.UtcDateTime.AddDays(7))
                            .OrderBy(x => x.PublishedAt.Value)
                            .FirstOrDefault();

                        if (firstStoreVideo != null)
                        {
                            media[firstStoreVideo.PublishedAt.Value] = firstStoreVideo.Id;
                            /*
                            try
                            {
                                await googleClient.LikeVideoAsync(storeVideo.Id);
                                await googleClient.CommentOnVideoAsync(storeVideo.ChannelId, storeVideo.Id,
                                    $"thank you for showcasing this weeks Rust skins, your video has been featured on https://scmm.app/store/{itemStore.Start.ToString(Constants.SCMMStoreIdDateFormat)}"
                                );
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, $"Failed to like and comment on new store video (channelId: {storeVideo.ChannelId}, videoId: {storeVideo.Id}), skipping...");
                            }
                            */
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to channel (id: {channel.ChannelId}) for new store videos, skipping...");
                    }
                }

                var newMedia = media
                    .Where(x => !itemStore.Media.Contains(x.Value))
                    .OrderBy(x => x.Key)
                    .ToList();

                if (newMedia.Any())
                {
                    logger.LogInformation($"{newMedia.Count} new video(s) were found for store {itemStore.Start.UtcDateTime}");
                    itemStore.Media = new PersistableStringCollection(
                        itemStore.Media.Union(newMedia.Select(x => x.Value))
                    );

                    _db.SaveChanges();
                    logger.LogInformation($"{itemStore.Media.Count} total video(s) are now recorded for store {itemStore.Start.UtcDateTime}");
                }
                else
                {
                    logger.LogInformation($"No new videos were found for store {itemStore.Start.UtcDateTime}");
                }
            }
        }
    }
}
