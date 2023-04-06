using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.WebApi.Requests.ISteamUser;
using SCMM.Steam.Data.Store;

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
        private readonly IServiceBus _serviceBus;
        private readonly SteamConfiguration _steamConfiguration;
        private readonly SteamDbContext _steamDb;
        private readonly SteamWebApiClient _steamWebApiClient;
        private readonly ProxiedSteamCommunityWebClient _steamCommunityClient;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamProfileFriends(ILogger<ImportSteamProfileFriends> logger, IServiceBus serviceBus, SteamDbContext steamDb, SteamWebApiClient steamWebApiClient, ProxiedSteamCommunityWebClient steamCommunityClient, IConfiguration cfg, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _serviceBus = serviceBus;
            _steamConfiguration = cfg?.GetSteamConfiguration();
            _steamDb = steamDb;
            _steamWebApiClient = steamWebApiClient;
            _steamCommunityClient = steamCommunityClient;
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
                var friendsListResponse = await _steamWebApiClient.SteamUserGetFriendList(new GetFriendListJsonRequest()
                {
                    SteamId = steamId.ToString()
                });
                if (friendsListResponse?.Friends == null)
                {
                    throw new ArgumentException(nameof(request), "SteamID is invalid, or profile no longer exists");
                }

                friendSteamIds = friendsListResponse.Friends
                    .Where(x => !String.IsNullOrEmpty(x.SteamId))
                    .Select(x => x.SteamId)
                    .ToArray();
                var existingFriendSteamIds = await _steamDb.SteamProfiles
                    .Where(x => friendSteamIds.Contains(x.SteamId))
                    .Select(x => x.SteamId)
                    .ToListAsync();
                var missingFriendSteamIds = friendSteamIds
                    .Except(existingFriendSteamIds)
                    .ToArray();

                var apps = await _steamDb.SteamApps.AsNoTracking()
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
                            var inventory = await _steamCommunityClient.GetInventoryPaginated(new SteamInventoryPaginatedJsonRequest()
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
                                await _serviceBus.SendMessageAsync(new ImportProfileMessage()
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

            if (profile != null)
            {
                profile.LastUpdatedFriendsOn = DateTimeOffset.UtcNow;
                await _steamDb.SaveChangesAsync();
            }

            return new ImportSteamProfileFriendsResponse
            {
                Profile = profile,
                FriendSteamIds = friendSteamIds
            };
        }
    }
}
