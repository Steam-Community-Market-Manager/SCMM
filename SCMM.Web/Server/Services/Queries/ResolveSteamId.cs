using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Shared;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Queries
{
    public class ResolveSteamIdRequest : IQuery<ResolveSteamIdResponse>
    {
        public string Id { get; set; }
    }

    public class ResolveSteamIdResponse
    {
        public Guid? Id { get; set; }

        public string SteamId { get; set; }

        public string ProfileId { get; set; }

        public bool Exists => (Id != null && Id != Guid.Empty);
    }

    public class ResolveSteamId : IQueryHandler<ResolveSteamIdRequest, ResolveSteamIdResponse>
    {
        private readonly ScmmDbContext _db;

        public ResolveSteamId(ScmmDbContext db)
        {
            _db = db;
        }

        public async Task<ResolveSteamIdResponse> HandleAsync(ResolveSteamIdRequest request)
        {
            Guid? id = null;
            ulong steamId = 0;
            string profileId = null;
            SteamProfile profile = null;
            if (string.IsNullOrEmpty(request.Id))
            {
                return null;
            }

            // Is this a int64 steam id?
            // e.g. 76561198082101518
            if (long.TryParse(request.Id, out _))
            {
                ulong.TryParse(request.Id, out steamId);
            }

            // Else, is this a profile page url containing a string steam id?
            // e.g. https://steamcommunity.com/profiles/76561198082101518/
            else if (Regex.IsMatch(request.Id, SteamConstants.SteamProfileUrlSteamIdRegex))
            {
                ulong.TryParse(Regex.Match(request.Id, SteamConstants.SteamProfileUrlSteamIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? request.Id, out steamId);
            }

            // Else, is this a profile page url containing a custom string profile id...
            // e.g. https://steamcommunity.com/id/bipolar_penguin/
            else if (Regex.IsMatch(request.Id, SteamConstants.SteamProfileUrlProfileIdRegex))
            {
                profileId = Regex.Match(request.Id, SteamConstants.SteamProfileUrlProfileIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? request.Id;
            }

            // Else, assume this is a custom string profile id...
            // e.g. bipolar_penguin
            else
            {
                profileId = request.Id;
            }

            // Look up the profiles true info if it exists in the database already
            if (steamId > 0)
            {
                profile = _db.SteamProfiles.AsNoTracking().FirstOrDefault(x => x.SteamId == steamId.ToString());
            }
            else if (!string.IsNullOrEmpty(profileId))
            {
                profile = _db.SteamProfiles.AsNoTracking().FirstOrDefault(x => x.ProfileId == profileId);
            }
            if (profile != null)
            {
                id = profile.Id;
                ulong.TryParse(profile.SteamId, out steamId);
                profileId = profile.ProfileId;
            }

            return new ResolveSteamIdResponse
            {
                Id = id,
                SteamId = steamId > 0 ? steamId.ToString() : null,
                ProfileId = profileId
            };
        }
    }
}
