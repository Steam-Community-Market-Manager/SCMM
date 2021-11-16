using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Store;
using System.Linq.Expressions;
using System.Security.Claims;

namespace SCMM.Web.Server.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool Is(this ClaimsPrincipal user, SteamProfile profile)
        {
            return (user.Identity.IsAuthenticated && user.Id() == profile.Id);
        }

        public static bool Is(this ClaimsPrincipal user, Guid profileId)
        {
            return (user.Identity.IsAuthenticated && user.Id() == profileId);
        }

        public static Guid Id(this ClaimsPrincipal user)
        {
            Guid.TryParse(user?.FindFirst(SCMM.Shared.Data.Models.ClaimTypes.Id)?.Value, out var id);
            return id;
        }

        public static string SteamId(this ClaimsPrincipal user)
        {
            return user?.FindFirst(SCMM.Shared.Data.Models.ClaimTypes.SteamId)?.Value;
        }

        public static string Name(this ClaimsPrincipal user)
        {
            return user?.FindFirst(SCMM.Shared.Data.Models.ClaimTypes.Name)?.Value ?? user.Identity?.Name;
        }

        public static string Language(this ClaimsPrincipal user)
        {
            return user?.FindFirst(SCMM.Shared.Data.Models.ClaimTypes.Language)?.Value;
        }

        public static string Currency(this ClaimsPrincipal user)
        {
            return user?.FindFirst(SCMM.Shared.Data.Models.ClaimTypes.Currency)?.Value;
        }

        public static T Preference<T>(this ClaimsPrincipal user, SteamDbContext db, Expression<Func<SteamProfile, T>> preference)
        {
            var profile = new SteamProfile();
            if (!Guid.TryParse(user?.FindFirst(SCMM.Shared.Data.Models.ClaimTypes.Id)?.Value, out var profileId))
            {
                profile = db.SteamProfiles.Local.FirstOrDefault(x => x.Id == profileId) ?? 
                          db.SteamProfiles.FirstOrDefault(x => x.Id == profileId);
            }

            return preference.Compile().Invoke(profile);
        }

    }
}
