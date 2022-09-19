using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Azure.ServiceBus;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
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
        private readonly SteamConfiguration _cfg;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamProfileFriends(ILogger<ImportSteamProfileFriends> logger, ServiceBusClient serviceBusClient, SteamDbContext db, IConfiguration cfg, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _serviceBusClient = serviceBusClient;
            _db = db;
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

                // Only import friends that own one of the apps we track
                // TODO: Reconsider this, we might find more inventories if we check everybody
                var apps = await _db.SteamApps.AsNoTracking().Where(x => x.IsActive).Select(x => x.SteamId).ToListAsync();
                var missingFriendSteamIdsWithPublicAppInventories = new List<string>();
                var playerService = steamWebInterfaceFactory.CreateSteamWebInterface<PlayerService>();
                foreach (var missingFriendSteamId in missingFriendSteamIds)
                {
                    var ownedApps = await playerService.GetOwnedGamesAsync(
                        UInt64.Parse(missingFriendSteamId),
                        appIdsToFilter: apps.Select(x => UInt32.Parse(x)).ToArray()
                    );
                    if (ownedApps?.Data != null && ownedApps?.Data?.OwnedGames?.Any(x => apps.Contains(x.AppId.ToString())) == true)
                    {
                        missingFriendSteamIdsWithPublicAppInventories.Add(missingFriendSteamId);
                    }
                }

                if (missingFriendSteamIdsWithPublicAppInventories.Any())
                {
                    await _serviceBusClient.SendMessagesAsync(
                        missingFriendSteamIdsWithPublicAppInventories.Select(x => new ImportProfileMessage()
                        {
                            ProfileId = x.ToString()
                        })
                    );
                }
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
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
