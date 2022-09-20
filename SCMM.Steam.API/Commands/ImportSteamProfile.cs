using CommandQuery;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Azure.ServiceBus;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Store;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Text.RegularExpressions;

namespace SCMM.Steam.API.Commands
{
    public class ImportSteamProfileRequest : ICommand<ImportSteamProfileResponse>
    {
        public string ProfileId { get; set; }

        /// <summary>
        /// If true, we'll queue a job to import their friends list too
        /// </summary>
        public bool ImportFriendsListAsync { get; set; } = false;

        /// <summary>
        /// If true, we'll queue a job to import their inventory too
        /// </summary>
        public bool ImportInventoryAsync { get; set; } = false;

        /// <summary>
        /// If true, profile will always be fetched. If false, profile is cached for 1 day.
        /// </summary>
        public bool Force { get; set; } = false;
    }

    public class ImportSteamProfileResponse
    {
        /// <remarks>
        /// If profile does not exist, this will be null
        /// </remarks>
        public SteamProfile Profile { get; set; }
    }

    public class ImportSteamProfile : ICommandHandler<ImportSteamProfileRequest, ImportSteamProfileResponse>
    {
        private readonly ILogger<ImportSteamProfile> _logger;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly SteamCommunityWebClient _communityClient;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamProfile(ILogger<ImportSteamProfile> logger, ServiceBusClient serviceBusClient, SteamDbContext db, IConfiguration cfg, SteamCommunityWebClient communityClient, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _serviceBusClient = serviceBusClient;
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _communityClient = communityClient;
            _queryProcessor = queryProcessor;
        }

        public async Task<ImportSteamProfileResponse> HandleAsync(ImportSteamProfileRequest request)
        {
            // Resolve the id
            var profile = (SteamProfile)null;
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = request.ProfileId
            });

            try
            {
                // If the profile is less than 1 day old and we aren't forcing an update, just return the current profile
                if (resolvedId?.Profile?.LastUpdatedOn != null && (DateTimeOffset.Now - resolvedId.Profile.LastUpdatedOn.Value) <= TimeSpan.FromDays(1) && request.Force == false)
                {
                    return new ImportSteamProfileResponse()
                    {
                        Profile = resolvedId?.Profile
                    };
                }

                // If we know the exact steam id, fetch using the Steam API
                if (resolvedId?.SteamId64 != null)
                {
                    _logger.LogInformation($"Importing profile '{resolvedId.SteamId64}' from Steam");

                    var steamId = resolvedId.SteamId64.Value;
                    var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);
                    var steamUser = steamWebInterfaceFactory.CreateSteamWebInterface<SteamUser>();
                    var playerSummaryResponse = await steamUser.GetPlayerSummaryAsync(steamId);
                    if (playerSummaryResponse?.Data == null)
                    {
                        throw new ArgumentException(nameof(request), "SteamID is invalid, or profile no longer exists");
                    }

                    var profileId = playerSummaryResponse.Data.ProfileUrl;
                    if (string.IsNullOrEmpty(profileId))
                    {
                        throw new ArgumentException(nameof(request), "ProfileID is invalid");
                    }

                    if (Regex.IsMatch(profileId, Constants.SteamProfileUrlSteamId64Regex))
                    {
                        profileId = Regex.Match(profileId, Constants.SteamProfileUrlSteamId64Regex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? profileId;
                    }
                    else if (Regex.IsMatch(profileId, Constants.SteamProfileUrlCustomUrlRegex))
                    {
                        profileId = Regex.Match(profileId, Constants.SteamProfileUrlCustomUrlRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? profileId;
                    }

                    profile = resolvedId.Profile ?? new SteamProfile()
                    {
                        SteamId = steamId.ToString()
                    };

                    profile.SteamId = steamId.ToString();
                    profile.ProfileId = profileId;
                    profile.Name = playerSummaryResponse.Data.Nickname?.Trim();
                    profile.AvatarUrl = playerSummaryResponse.Data.AvatarMediumUrl;
                    profile.AvatarLargeUrl = playerSummaryResponse.Data.AvatarFullUrl;

                    var playerBanResponse = await steamUser.GetPlayerBansAsync(steamId);
                    var playerBan = playerBanResponse?.Data?.FirstOrDefault(x => x.SteamId == steamId.ToString());
                    if (playerBan != null)
                    {
                        profile.IsTradeBanned = (
                            !String.IsNullOrEmpty(playerBan.EconomyBan) && 
                            !String.Equals(playerBan.EconomyBan, "none", StringComparison.InvariantCultureIgnoreCase)
                        );
                    }
                }

                // Else, if we know the custom profile id, fetch using the legacy XML API
                else if (!string.IsNullOrEmpty(resolvedId?.CustomUrl))
                {
                    _logger.LogInformation($"Importing profile '{resolvedId.CustomUrl}' from Steam");

                    var profileId = resolvedId.CustomUrl;
                    var response = await _communityClient.GetProfileById(new SteamProfileByIdPageRequest()
                    {
                        ProfileId = profileId,
                        Xml = true
                    });
                    if (response == null)
                    {
                        throw new ArgumentException(nameof(request), "ProfileID is invalid, or profile no longer exists");
                    }

                    var steamId = response.SteamID64.ToString();
                    if (string.IsNullOrEmpty(steamId))
                    {
                        throw new ArgumentException(nameof(request), "SteamID is invalid");
                    }

                    // Resolve the profile again using the steam id.
                    // This handles the case where an existing profile has changed their custom url name
                    resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
                    {
                        Id = steamId
                    });

                    profile = resolvedId.Profile ?? new SteamProfile()
                    {
                        ProfileId = profileId
                    };

                    profile.SteamId = response.SteamID64.ToString();
                    profile.ProfileId = profileId;
                    profile.Name = response.SteamID.Trim();
                    profile.AvatarUrl = response.AvatarMedium;
                    profile.AvatarLargeUrl = response.AvatarFull;
                    profile.IsTradeBanned = (
                        !String.IsNullOrEmpty(response.TradeBanState) && 
                        !String.Equals(response.TradeBanState, "none", StringComparison.InvariantCultureIgnoreCase)
                    );
                }

                // Save the new profile to the datbase
                if (profile != null && profile.Id == Guid.Empty)
                {
                    _db.SteamProfiles.Add(profile);
                }

                profile.LastUpdatedOn = DateTimeOffset.Now;

                await _db.SaveChangesAsync();

                if (request.ImportFriendsListAsync && profile != null && !String.IsNullOrEmpty(profile.SteamId))
                {
                    await _serviceBusClient.SendMessageAsync(new ImportProfileFriendsMessage()
                    {
                        ProfileId = profile.SteamId
                    });
                }
                if (request.ImportInventoryAsync && profile != null && !String.IsNullOrEmpty(profile.SteamId))
                {
                    await _serviceBusClient.SendMessageAsync(new ImportProfileInventoryMessage()
                    {
                        ProfileId = profile.SteamId
                    });
                }
            }
            catch (SteamRequestException ex)
            {
                if (ex.Error?.Message?.Contains("profile could not be found", StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    profile = null;
                }
                else
                {
                    throw;
                }
            }

            return new ImportSteamProfileResponse
            {
                Profile = profile
            };
        }
    }
}
