using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Azure.ServiceBus;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Store;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;

namespace SCMM.Steam.API.Commands
{
    public class ImportSteamProfileFriendsRequest : ICommand<ImportSteamProfileFriendsResponse>
    {
        public string ProfileId { get; set; }

        /// <summary>
        /// If true, we'll queue a job to import their inventory too
        /// </summary>
        public bool ImportInventoryAsync { get; set; } = false;
    }

    public class ImportSteamProfileFriendsResponse
    {
        public SteamProfile Profile { get; set; }

        public IEnumerable<string> FriendSteamIds { get; set; }
    }

    public class ImportSteamProfileFriends : ICommandHandler<ImportSteamProfileFriendsRequest, ImportSteamProfileFriendsResponse>
    {
        private readonly ILogger<ImportSteamProfileFriends> _logger;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly SteamDbContext _db;
        private readonly SteamCommunityWebClient _communityClient;
        private readonly SteamConfiguration _cfg;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamProfileFriends(ILogger<ImportSteamProfileFriends> logger, ServiceBusClient serviceBusClient, SteamDbContext db, SteamCommunityWebClient communityClient, IConfiguration cfg, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _serviceBusClient = serviceBusClient;
            _db = db;
            _communityClient = communityClient;
            _cfg = cfg?.GetSteamConfiguration();
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public async Task<ImportSteamProfileFriendsResponse> HandleAsync(ImportSteamProfileFriendsRequest request)
        {
            // Resolve the id
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = request.ProfileId
            });

            var friendSteamIds = new string[0];
            var profile = resolvedId.Profile;
            if (profile == null)
            {
                return null;
            }

            try
            {
                _logger.LogInformation($"Importing friends of '{resolvedId.SteamId64}' from Steam");

                var steamId = resolvedId.SteamId64.Value;
                var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);
                var steamUser = steamWebInterfaceFactory.CreateSteamWebInterface<SteamUser>();
                var friendsListResponse = await steamUser.GetFriendsListAsync(steamId);
                if (friendsListResponse?.Data == null)
                {
                    throw new ArgumentException(nameof(request), "SteamID is invalid, or profile no longer exists");
                }

                friendSteamIds = friendsListResponse.Data
                    .Where(x => x.SteamId > 0)
                    .Select(x => x.SteamId.ToString())
                    .ToArray();
                var existingFriendSteamIds = await _db.SteamProfiles
                    .Where(x => friendSteamIds.Contains(x.SteamId))
                    .Select(x => x.SteamId)
                    .ToListAsync();
                var missingFriendSteamIds = friendSteamIds
                    .Except(existingFriendSteamIds)
                    .ToArray();

                var apps = await _db.SteamApps.AsNoTracking()
                    .Where(x => x.IsActive)
                    .Select(x => x.SteamId)
                    .ToListAsync();

                foreach (var missingFriendSteamId in missingFriendSteamIds)
                {
                    try
                    {
                        foreach (var app in apps)
                        {
                            // Only import profiles that have a public inventory containing items from at least one of our active apps
                            var inventory = await _communityClient.GetInventoryPaginated(new SteamInventoryPaginatedJsonRequest()
                            {
                                AppId = app,
                                SteamId = missingFriendSteamId,
                                StartAssetId = null,
                                Count = SteamInventoryPaginatedJsonRequest.MaxPageSize,
                                NoRender = true
                            });
                            if (inventory?.Assets?.Any() == true)
                            {
                                // Inventory found, import this profile
                                await _serviceBusClient.SendMessageAsync(new ImportProfileMessage()
                                {
                                    ProfileId = missingFriendSteamId.ToString()
                                });
                                break;
                            }
                        }
                    }
                    catch (SteamRequestException ex)
                    {
                        if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden || ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            // Inventory is probably private, skip them...
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                profile.LastUpdatedFriendsOn = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden || ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning(ex, $"Failed to get friend list, unauthorised, probably private");
                }
                else
                {
                    throw;
                }
            }

            return new ImportSteamProfileFriendsResponse
            {
                Profile = profile,
                FriendSteamIds = friendSteamIds
            };
        }
    }
}
