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
using SCMM.Web.Server.Services.Queries;
using SCMM.Web.Shared.Domain.DTOs.Profiles;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Commands.FetchAndCreateSteamProfile
{
    public class FetchAndCreateSteamProfileRequest : ICommand<FetchAndCreateSteamProfileResponse>
    {
        public string Id { get; set; }
    }

    public class FetchAndCreateSteamProfileResponse
    {
        public ProfileDTO Profile { get; set; }
    }

    public class FetchAndCreateSteamProfile : ICommandHandler<FetchAndCreateSteamProfileRequest, FetchAndCreateSteamProfileResponse>
    {
        private readonly ScmmDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly SteamCommunityClient _communityClient;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public FetchAndCreateSteamProfile(ScmmDbContext db, IConfiguration cfg, SteamCommunityClient communityClient, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _communityClient = communityClient;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        public async Task<FetchAndCreateSteamProfileResponse> HandleAsync(FetchAndCreateSteamProfileRequest request)
        {
            // Resolve the id
            var profile = (SteamProfile) null;
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = request.Id
            });

            // If we know the exact steam id, fetch using the Steam API
            if (!String.IsNullOrEmpty(resolvedId.SteamId))
            {
                var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);
                var steamUser = steamWebInterfaceFactory.CreateSteamWebInterface<SteamUser>();
                var response = await steamUser.GetPlayerSummaryAsync(UInt64.Parse(resolvedId.SteamId));
                if (response?.Data == null)
                {
                    throw new Exception("No response received from server");
                }

                var profileId = response.Data.ProfileUrl;
                if (!string.IsNullOrEmpty(profileId))
                {
                    if (Regex.IsMatch(profileId, SteamConstants.SteamProfileUrlSteamIdRegex))
                    {
                        profileId = Regex.Match(profileId, SteamConstants.SteamProfileUrlSteamIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? profileId;
                    }
                    else if (Regex.IsMatch(profileId, SteamConstants.SteamProfileUrlProfileIdRegex))
                    {
                        profileId = Regex.Match(profileId, SteamConstants.SteamProfileUrlProfileIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? profileId;
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
                    throw new Exception("No response received from server");
                }

                profile = await _db.SteamProfiles.FirstOrDefaultAsync(
                    x => x.ProfileId == resolvedId.ProfileId
                );
                profile = profile ?? new SteamProfile()
                {
                    ProfileId = resolvedId.ProfileId
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
