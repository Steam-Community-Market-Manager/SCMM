using CommandQuery;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Steam.API.Queries
{
    public class ResolveSteamIdRequest : IQuery<ResolveSteamIdResponse>
    {
        public string Id { get; set; }
    }

    public class ResolveSteamIdResponse
    {
        public SteamProfile Profile { get; set; }

        public Guid? ProfileId { get; set; }

        public ulong? SteamId64 { get; set; }

        public string CustomUrl { get; set; }

        public bool Exists => (Profile != null);
    }

    public class ResolveSteamId : IQueryHandler<ResolveSteamIdRequest, ResolveSteamIdResponse>
    {
        private readonly SteamDbContext _db;

        public ResolveSteamId(SteamDbContext db)
        {
            _db = db;
        }

        public async Task<ResolveSteamIdResponse> HandleAsync(ResolveSteamIdRequest request, CancellationToken cancellationToken)
        {
            SteamProfile profile = null;
            Guid profileId = Guid.Empty;
            ulong steamId64 = 0;
            string customUrl = null;
            if (string.IsNullOrEmpty(request.Id))
            {
                return null;
            }

            // Is this a guid id?
            if (Guid.TryParse(request.Id, out profileId))
            {
            }

            // Is this a int64 steam id?
            // e.g. 76561198082101518
            else if (long.TryParse(request.Id, out _))
            {
                ulong.TryParse(request.Id, out steamId64);
            }

            // Else, is this a profile page url containing a string steam id?
            // e.g. https://steamcommunity.com/profiles/76561198082101518/
            else if (Regex.IsMatch(request.Id, Constants.SteamProfileUrlSteamId64Regex))
            {
                ulong.TryParse(Regex.Match(request.Id, Constants.SteamProfileUrlSteamId64Regex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? request.Id, out steamId64);
            }

            // Else, is this a profile page url containing a custom string profile id...
            // e.g. https://steamcommunity.com/id/bipolar_penguin/
            else if (Regex.IsMatch(request.Id, Constants.SteamProfileUrlCustomUrlRegex))
            {
                customUrl = Regex.Match(request.Id, Constants.SteamProfileUrlCustomUrlRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? request.Id;
            }

            // Else, assume this is a custom string profile id...
            // e.g. bipolar_penguin
            else
            {
                customUrl = request.Id;
            }

            // Look up the profiles true info if it exists in the database already (use local cache first, to reduce round-trips)
            if (profileId != Guid.Empty)
            {
                profile = _db.SteamProfiles.Local.FirstOrDefault(x => x.Id == profileId) ??
                          _db.SteamProfiles.FirstOrDefault(x => x.Id == profileId);
            }
            else if (steamId64 > 0)
            {
                profile = _db.SteamProfiles.Local.FirstOrDefault(x => x.SteamId == steamId64.ToString()) ??
                          _db.SteamProfiles.FirstOrDefault(x => x.SteamId == steamId64.ToString());
            }
            else if (!string.IsNullOrEmpty(customUrl))
            {
                profile = _db.SteamProfiles.Local.FirstOrDefault(x => x.ProfileId == customUrl) ??
                          _db.SteamProfiles.FirstOrDefault(x => x.ProfileId == customUrl);
            }

            if (profile != null)
            {
                profileId = profile.Id;
                ulong.TryParse(profile.SteamId, out steamId64);
                customUrl = profile.ProfileId;
            }

            return new ResolveSteamIdResponse
            {
                Profile = profile,
                ProfileId = profileId != Guid.Empty ? profileId : null,
                SteamId64 = steamId64 > 0 ? steamId64 : null,
                CustomUrl = customUrl
            };
        }
    }
}
