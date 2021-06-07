using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
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
    public class ImportSteamProfileRequest : ICommand<ImportSteamProfileResponse>
    {
        public string ProfileId { get; set; }
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
        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly SteamCommunityWebClient _communityClient;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamProfile(ILogger<ImportSteamProfile> logger, SteamDbContext db, IConfiguration cfg, SteamCommunityWebClient communityClient, IQueryProcessor queryProcessor)
        {
            _logger = logger;
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
                // If we know the exact steam id, fetch using the Steam API
                if (!String.IsNullOrEmpty(resolvedId.SteamId))
                {
                    var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);
                    var steamUser = steamWebInterfaceFactory.CreateSteamWebInterface<SteamUser>();
                    var response = await steamUser.GetPlayerSummaryAsync(UInt64.Parse(resolvedId.SteamId));
                    if (response?.Data == null)
                    {
                        throw new ArgumentException(nameof(request), "SteamID is invalid, or profile no longer exists");
                    }

                    var profileId = response.Data.ProfileUrl;
                    if (String.IsNullOrEmpty(profileId))
                    {
                        throw new ArgumentException(nameof(request), "ProfileID is invalid");
                    }

                    if (Regex.IsMatch(profileId, Constants.SteamProfileUrlSteamIdRegex))
                    {
                        profileId = Regex.Match(profileId, Constants.SteamProfileUrlSteamIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? profileId;
                    }
                    else if (Regex.IsMatch(profileId, Constants.SteamProfileUrlProfileIdRegex))
                    {
                        profileId = Regex.Match(profileId, Constants.SteamProfileUrlProfileIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? profileId;
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
                        throw new ArgumentException(nameof(request), "ProfileID is invalid, or profile no longer exists");
                    }

                    var steamId = response.SteamID64.ToString();
                    if (String.IsNullOrEmpty(steamId))
                    {
                        throw new ArgumentException(nameof(request), "SteamID is invalid");
                    }

                    profile = await _db.SteamProfiles.FirstOrDefaultAsync(
                        x => x.SteamId == steamId
                    );
                    profile = profile ?? new SteamProfile()
                    {
                        ProfileId = resolvedId.ProfileId
                    };

                    profile.SteamId = response.SteamID64.ToString();
                    profile.ProfileId = resolvedId.ProfileId;
                    profile.Name = response.SteamID.Trim();
                    profile.AvatarUrl = response.AvatarMedium;
                    profile.AvatarLargeUrl = response.AvatarFull;
                    profile.Country = response.Location;
                }

                // Save the new profile to the datbase
                if (profile != null && profile.Id == Guid.Empty)
                {
                    _db.SteamProfiles.Add(profile);
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
