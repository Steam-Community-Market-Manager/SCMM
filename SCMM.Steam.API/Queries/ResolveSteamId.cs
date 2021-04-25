using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
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
        private readonly SteamDbContext _db;

        public ResolveSteamId(SteamDbContext db)
        {
            _db = db;
        }

        public Task<ResolveSteamIdResponse> HandleAsync(ResolveSteamIdRequest request)
        {
            return Task.Run(() =>
            {
                Guid id = Guid.Empty;
                ulong steamId = 0;
                string profileId = null;
                SteamProfile profile = null;
                if (string.IsNullOrEmpty(request.Id))
                {
                    return null;
                }

                // Is this a guid id?
                if (Guid.TryParse(request.Id, out id))
                {
                }

                // Is this a int64 steam id?
                // e.g. 76561198082101518
                else if (long.TryParse(request.Id, out _))
                {
                    ulong.TryParse(request.Id, out steamId);
                }

                // Else, is this a profile page url containing a string steam id?
                // e.g. https://steamcommunity.com/profiles/76561198082101518/
                else if (Regex.IsMatch(request.Id, Constants.SteamProfileUrlSteamIdRegex))
                {
                    ulong.TryParse(Regex.Match(request.Id, Constants.SteamProfileUrlSteamIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? request.Id, out steamId);
                }

                // Else, is this a profile page url containing a custom string profile id...
                // e.g. https://steamcommunity.com/id/bipolar_penguin/
                else if (Regex.IsMatch(request.Id, Constants.SteamProfileUrlProfileIdRegex))
                {
                    profileId = Regex.Match(request.Id, Constants.SteamProfileUrlProfileIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? request.Id;
                }

                // Else, assume this is a custom string profile id...
                // e.g. bipolar_penguin
                else
                {
                    profileId = request.Id;
                }

                // Look up the profiles true info if it exists in the database already
                if (id != Guid.Empty)
                {
                    profile = _db.SteamProfiles.AsNoTracking().FirstOrDefault(x => x.Id == id);
                }
                else if (steamId > 0)
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
                    Id = id != Guid.Empty ? id : null,
                    SteamId = steamId > 0 ? steamId.ToString() : null,
                    ProfileId = profileId
                };
            });
        }
    }
}
