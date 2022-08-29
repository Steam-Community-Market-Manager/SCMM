using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Azure.ServiceBus;
using SCMM.Google.Client;
using SCMM.Shared.API.Messages;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using System.Text.RegularExpressions;

namespace SCMM.Steam.Functions.Timer;

public class CheckNewStoreVideosYouTube
{
    private readonly SteamDbContext _db;
    private readonly GoogleClient _googleClient;
    private readonly CheckNewStoreVideosConfiguration _configuration;
    private readonly ServiceBusClient _serviceBus;

    public CheckNewStoreVideosYouTube(IConfiguration configuration, SteamDbContext db, GoogleClient googleClient, ServiceBusClient serviceBus)
    {
        _db = db;
        _googleClient = googleClient;
        _configuration = configuration.GetSection("StoreVideos").Get<CheckNewStoreVideosConfiguration>();
        _serviceBus = serviceBus;
    }

    [Function("Check-New-Store-Videos-YouTube")]
    public async Task Run([TimerTrigger("0 5 * * * *")] /* every hour, 5mins past */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-Store-Videos-YouTube");

        var steamApps = await _db.SteamApps
            .Where(x => x.Features.HasFlag(SteamAppFeatureTypes.StoreRotating))
            .Where(x => x.IsActive)
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
                var media = new List<YouTubeVideo>();
                foreach (var channel in _configuration.Channels.Where(x => x.Type == CheckNewStoreVideosConfiguration.ChannelType.YouTube))
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
                        var videos = await _googleClient.ListChannelVideosAsync(channel.ChannelId, GoogleClient.PageMaxResults);
                        var firstStoreVideo = videos
                            .Where(x => Regex.IsMatch(x.Title, channel.Query, RegexOptions.IgnoreCase))
                            .Where(x => x.PublishedAt != null && x.PublishedAt.Value.UtcDateTime >= itemStore.Start.Value.UtcDateTime && x.PublishedAt.Value.UtcDateTime <= itemStore.Start.Value.UtcDateTime.AddDays(7))
                            .OrderBy(x => x.PublishedAt.Value)
                            .FirstOrDefault();

                        if (firstStoreVideo != null)
                        {
                            media.Add(firstStoreVideo);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to check channel (id: {channel.ChannelId}) for new store videos, skipping...");
                    }
                }

                var newMedia = media
                    .Where(x => !itemStore.Media.Contains(x.Id))
                    .OrderBy(x => x.PublishedAt)
                    .ToList();

                if (newMedia.Any())
                {
                    logger.LogInformation($"{newMedia.Count} new video(s) were found for store {itemStore.Start.Value.UtcDateTime}");
                    itemStore.Media = new PersistableStringCollection(
                        itemStore.Media.Union(newMedia.Select(x => x.Id))
                    );

                    await _db.SaveChangesAsync();
                    
                    foreach (var item in newMedia)
                    {
                        await _serviceBus.SendMessageAsync(new StoreMediaAddedMessage()
                        {
                            StoreStartedOn = itemStore.Start,
                            ChannelId = item.ChannelId,
                            ChannelTitle = item.ChannelTitle,
                            VideoId = item.Id,
                            VideoTitle = item.Title,
                            VideoThumbnailUrl = item.Thumbnail.ToString(),
                            VideoPublishedOn = item.PublishedAt ?? DateTimeOffset.Now,
                        });

                        /*
                        try
                        {
                            await googleClient.LikeVideoAsync(storeVideo.Id);
                            await googleClient.CommentOnVideoAsync(storeVideo.ChannelId, storeVideo.Id,
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
