using SCMM.Web.Server.Data.Models.Steam;
using System;
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
            Guid id;
            Guid.TryParse(user?.FindFirst(SCMM.Web.Shared.Data.Models.ClaimTypes.Id)?.Value, out id);
            return id;
        }

        public static string SteamId(this ClaimsPrincipal user)
        {
            return user?.FindFirst(SCMM.Web.Shared.Data.Models.ClaimTypes.SteamId)?.Value;
        }

        public static string Name(this ClaimsPrincipal user)
        {
            return user?.FindFirst(SCMM.Web.Shared.Data.Models.ClaimTypes.Name)?.Value ?? user.Identity?.Name;
        }

        public static string Language(this ClaimsPrincipal user)
        {
            return user?.FindFirst(SCMM.Web.Shared.Data.Models.ClaimTypes.Language)?.Value;
        }

        public static string Currency(this ClaimsPrincipal user)
        {
            return user?.FindFirst(SCMM.Web.Shared.Data.Models.ClaimTypes.Currency)?.Value;
        }
    }
}
