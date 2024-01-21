using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Media;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.API.Events;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using System.Text.RegularExpressions;

namespace SCMM.Steam.Functions.Timer;

public class CheckForNewStoreVideos
{
    private readonly SteamDbContext _db;
    private readonly IEnumerable<IVideoStreamingService> _videoStreamingServices;
    private readonly CheckForNewStoreVideosConfiguration _configuration;
    private readonly IServiceBus _serviceBus;

    public CheckForNewStoreVideos(IConfiguration configuration, SteamDbContext db, IEnumerable<IVideoStreamingService> videoStreamingServices, IServiceBus serviceBus)
    {
        _db = db;
        _videoStreamingServices = videoStreamingServices;
        _configuration = configuration.GetSection("StoreVideos").Get<CheckForNewStoreVideosConfiguration>();
        _serviceBus = serviceBus;
    }

    [Function("Check-New-Store-Videos")]
    public async Task Run([TimerTrigger("0 5 * * * *")] /* every hour, 5 minutes past */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-Store-Videos");

        var steamApps = await _db.SteamApps
            .Where(x => x.FeatureFlags.HasFlag(SteamAppFeatureFlags.ItemStoreMediaTracking))
            .ToListAsync();
        if (!steamApps.Any())
        {
            return;
        }

        foreach (var app in steamApps)
        {
            var activeItemStores = await _db.SteamItemStores
                .Where(x => x.AppId == app.Id)
                .Where(x => x.Start != null && x.End == null)
                .OrderByDescending(x => x.Start)
                .ToListAsync();

            foreach (var itemStore in activeItemStores)
            {
                var media = new Dictionary<IVideo, IVideoStreamingService>();
                foreach (var channel in _configuration.Channels.Where(x => x.AppId == app.SteamId && x.Type == CheckForNewStoreVideosConfiguration.ChannelType.YouTube))
                {
                    foreach (var videoStreamingService in _videoStreamingServices)
                    {
                        try
                        {
                            // TODO: If we already have a video for this channel, don't waste time checking again
                            // NOTE: We only accept one video per-channel, per-store
                            /*
                            if (itemStore.Media.ContainsKey(channel.ChannelId))
                            {
                                continue;
                            }
                            */

                            // Find the earliest video that matches our store data period.
                            logger.LogTrace($"Checking channel (id: {channel.ChannelId}) for new store videos since {itemStore.Start.Value.UtcDateTime}...");
                            var videos = await videoStreamingService.ListChannelVideosAsync(channel.ChannelId);
                            var firstStoreVideo = videos
                                .Where(x => Regex.IsMatch(x.Title, channel.Query, RegexOptions.IgnoreCase))
                                .Where(x => x.PublishedAt != null && x.PublishedAt.Value.UtcDateTime >= itemStore.Start.Value.UtcDateTime && x.PublishedAt.Value.UtcDateTime <= itemStore.Start.Value.UtcDateTime.AddDays(7))
                                .OrderBy(x => x.PublishedAt.Value)
                                .FirstOrDefault();

                            if (firstStoreVideo != null)
                            {
                                media[firstStoreVideo] = videoStreamingService;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Failed to check channel (id: {channel.ChannelId}) for new store videos, skipping...");
                        }
                    }
                }

                var newMedia = media
                    .Where(x => !itemStore.Media.Contains(x.Key.Id))
                    .OrderBy(x => x.Key.PublishedAt)
                    .ToList();

                if (newMedia.Any())
                {
                    logger.LogInformation($"{newMedia.Count} new video(s) were found for store {itemStore.Start.Value.UtcDateTime}");
                    itemStore.Media = new PersistableStringCollection(
                        itemStore.Media.Union(newMedia.Select(x => x.Key.Id))
                    );

                    await _db.SaveChangesAsync();

                    foreach (var item in newMedia)
                    {
                        await _serviceBus.SendMessageAsync(new StoreMediaAddedMessage()
                        {
                            AppId = UInt64.Parse(app.SteamId),
                            AppName = app.Name,
                            AppIconUrl = app.IconUrl,
                            AppColour = app.PrimaryColor,
                            StoreId = itemStore.StoreId(),
                            StoreName = itemStore.StoreName(),
                            ChannelId = item.Key.ChannelId,
                            ChannelName = item.Key.ChannelTitle,
                            VideoId = item.Key.Id,
                            VideoName = item.Key.Title,
                            VideoThumbnailUrl = item.Key.Thumbnail.ToString(),
                            VideoPublishedOn = item.Key.PublishedAt ?? DateTimeOffset.Now,
                        });

                        /*
                        try
                        {
                            await item.Value.LikeVideoAsync(storeVideo.Id);
                            await item.Value.CommentOnVideoAsync(storeVideo.ChannelId, storeVideo.Id,
                                $"thank you for showcasing this weeks new skins, your video has been featured on {_configuration.GetWebsiteUrl()}/store/{itemStore.Start.ToString(Constants.SCMMStoreIdDateFormat)}"
                            );
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, $"Failed to like and comment on new store video (channelId: {storeVideo.ChannelId}, videoId: {storeVideo.Id}), skipping...");
                        }
                        */
                    }
                }
                else
                {
                    logger.LogTrace($"No new videos were found for store {itemStore.Start.Value.UtcDateTime}");
                }
            }
        }
    }
}
