using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Store;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCMM.Steam.API.Commands
{
    public class FetchAndCreateSteamProfileRequest : ICommand<FetchAndCreateSteamProfileResponse>
    {
        public string ProfileId { get; set; }
    }

    public class FetchAndCreateSteamProfileResponse
    {
        public SteamProfile Profile { get; set; }
    }

    public class FetchAndCreateSteamProfile : ICommandHandler<FetchAndCreateSteamProfileRequest, FetchAndCreateSteamProfileResponse>
    {
        private readonly ILogger<FetchAndCreateSteamProfile> _logger;
        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly SteamCommunityClient _communityClient;
        private readonly IQueryProcessor _queryProcessor;

        public FetchAndCreateSteamProfile(ILogger<FetchAndCreateSteamProfile> logger, SteamDbContext db, IConfiguration cfg, SteamCommunityClient communityClient, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _communityClient = communityClient;
            _queryProcessor = queryProcessor;
        }

        public async Task<FetchAndCreateSteamProfileResponse> HandleAsync(FetchAndCreateSteamProfileRequest request)
        {
            // Resolve the id
            var profile = (SteamProfile)null;
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = request.ProfileId
            });

            try
            {
                // If we know the exact steam id, fetch using the Steam API
                if (!String.IsNullOrEmpty(resolvedId.SteamId))
                {
                    var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);
                    var steamUser = steamWebInterfaceFactory.CreateSteamWebInterface<SteamUser>();
                    var response = await steamUser.GetPlayerSummaryAsync(UInt64.Parse(resolvedId.SteamId));
                    if (response?.Data == null)
                    {
                        // Profile is probably private or deleted
                        throw new Exception("No response received from server");
                    }

                    var profileId = response.Data.ProfileUrl;
                    if (!string.IsNullOrEmpty(profileId))
                    {
                        if (Regex.IsMatch(profileId, Constants.SteamProfileUrlSteamIdRegex))
                        {
                            profileId = Regex.Match(profileId, Constants.SteamProfileUrlSteamIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? profileId;
                        }
                        else if (Regex.IsMatch(profileId, Constants.SteamProfileUrlProfileIdRegex))
                        {
                            profileId = Regex.Match(profileId, Constants.SteamProfileUrlProfileIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? profileId;
                        }
                    }

                    profile = await _db.SteamProfiles.FirstOrDefaultAsync(
                        x => x.SteamId == resolvedId.SteamId
                    );
                    profile = profile ?? new SteamProfile()
                    {
                        SteamId = resolvedId.SteamId
                    };

                    profile.ProfileId = profileId;
                    profile.Name = response.Data.Nickname?.Trim();
                    profile.AvatarUrl = response.Data.AvatarMediumUrl;
                    profile.AvatarLargeUrl = response.Data.AvatarFullUrl;
                    profile.Country = response.Data.CountryCode;
                }

                // Else, if we know the custom profile id, fetch using the legacy XML API
                else if (!String.IsNullOrEmpty(resolvedId.ProfileId))
                {
                    var response = await _communityClient.GetProfile(new SteamProfilePageRequest()
                    {
                        ProfileId = resolvedId.ProfileId,
                        Xml = true
                    });
                    if (response == null)
                    {
                        // Profile is probably private or deleted
                        throw new Exception("No response received from server");
                    }

                    profile = await _db.SteamProfiles.FirstOrDefaultAsync(
                        x => x.SteamId == response.SteamID64.ToString()
                    );
                    profile = profile ?? new SteamProfile()
                    {
                        ProfileId = resolvedId.ProfileId
                    };

                    profile.SteamId = response.SteamID64.ToString();
                    profile.ProfileId = resolvedId.ProfileId;
                    profile.Name = response.SteamID?.Trim();
                    profile.AvatarUrl = response.AvatarMedium;
                    profile.AvatarLargeUrl = response.AvatarFull;
                    profile.Country = response.Location;
                }

                // Save the new profile to the datbase
                if (profile != null && profile.Id == Guid.Empty)
                {
                    _db.SteamProfiles.Add(profile);
                }

                return new FetchAndCreateSteamProfileResponse
                {
                    Profile = profile
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get profile from Steam");
                return null;
            }
        }
    }
}
