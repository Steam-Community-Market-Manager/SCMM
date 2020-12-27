using AutoMapper;
using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Steam.Client;
using SCMM.Steam.Shared;
using SCMM.Steam.Shared.Community.Requests.Html;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Shared.Domain.DTOs.Profiles;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Commands.FetchAndCreateSteamProfile
{
    public class FetchAndCreateSteamProfileHandler : ICommandHandler<FetchAndCreateSteamProfileRequest, FetchAndCreateSteamProfileResponse>
    {
        private readonly ScmmDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly SteamCommunityClient _communityClient;
        private readonly IMapper _mapper;

        public FetchAndCreateSteamProfileHandler(ScmmDbContext db, IConfiguration cfg, SteamCommunityClient communityClient, IMapper mapper)
        {
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _communityClient = communityClient;
            _mapper = mapper;
        }

        public async Task<FetchAndCreateSteamProfileResponse> HandleAsync(FetchAndCreateSteamProfileRequest request)
        {
            var id = request.Id;
            var profile = (SteamProfile)null;
            var steamId = (ulong)0;
            var profileId = (string)null;
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            // Is this a int64 steam id?
            // e.g. 76561198082101518
            if (long.TryParse(id, out _))
            {
                ulong.TryParse(id, out steamId);
            }

            // Else, is this a profile page url containing a string steam id?
            // e.g. https://steamcommunity.com/profiles/76561198082101518/
            else if (Regex.IsMatch(id, SteamConstants.SteamProfileUrlSteamIdRegex))
            {
                ulong.TryParse(Regex.Match(id, SteamConstants.SteamProfileUrlSteamIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? id, out steamId);
            }

            // Else, is this a profile page url containing a custom string profile id...
            // e.g. https://steamcommunity.com/id/bipolar_penguin/
            else if (Regex.IsMatch(id, SteamConstants.SteamProfileUrlProfileIdRegex))
            {
                profileId = Regex.Match(id, SteamConstants.SteamProfileUrlProfileIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? id;
            }

            // Else, assume this is a custom string profile id...
            // e.g. bipolar_penguin
            else
            {
                profileId = id;
            }

            // If we know the exact steam id, fetch using the Steam API
            if (steamId > 0)
            {
                var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);
                var steamUser = steamWebInterfaceFactory.CreateSteamWebInterface<SteamUser>();
                var response = await steamUser.GetPlayerSummaryAsync(steamId);
                if (response?.Data == null)
                {
                    throw new Exception("No response received from server");
                }

                if (!string.IsNullOrEmpty(response.Data.ProfileUrl))
                {
                    if (Regex.IsMatch(id, SteamConstants.SteamProfileUrlSteamIdRegex))
                    {
                        profileId = Regex.Match(response.Data.ProfileUrl, SteamConstants.SteamProfileUrlSteamIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? profileId;
                    }
                    else if (Regex.IsMatch(id, SteamConstants.SteamProfileUrlProfileIdRegex))
                    {
                        profileId = Regex.Match(response.Data.ProfileUrl, SteamConstants.SteamProfileUrlProfileIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? profileId;
                    }
                }

                profile = await _db.SteamProfiles.FirstOrDefaultAsync(
                    x => x.SteamId == steamId.ToString()
                );
                profile = profile ?? new SteamProfile()
                {
                    SteamId = steamId.ToString()
                };

                profile.ProfileId = profileId;
                profile.Name = response.Data.Nickname?.Trim();
                profile.AvatarUrl = response.Data.AvatarMediumUrl;
                profile.AvatarLargeUrl = response.Data.AvatarFullUrl;
                profile.Country = response.Data.CountryCode;
            }

            // Else, if we know the custom profile id, fetch using the legacy XML API
            else if (!string.IsNullOrEmpty(profileId))
            {
                var response = await _communityClient.GetProfile(new SteamProfilePageRequest()
                {
                    ProfileId = profileId,
                    Xml = true
                });
                if (response == null)
                {
                    throw new Exception("No response received from server");
                }

                profile = await _db.SteamProfiles.FirstOrDefaultAsync(
                    x => x.ProfileId == profileId
                );
                profile = profile ?? new SteamProfile()
                {
                    ProfileId = profileId
                };

                profile.SteamId = response.SteamID64.ToString();
                profile.Name = response.SteamID?.Trim();
                profile.AvatarUrl = response.AvatarMedium;
                profile.AvatarLargeUrl = response.AvatarFull;
                profile.Country = response.Location;
            }

            if (profile != null)
            {
                if (profile.Id == Guid.Empty)
                {
                    _db.SteamProfiles.Add(profile);
                }
                _db.SaveChanges();
            }

            return new FetchAndCreateSteamProfileResponse
            {
                Profile = _mapper.Map<ProfileDetailedDTO>(profile)
            };
        }
    }
}
