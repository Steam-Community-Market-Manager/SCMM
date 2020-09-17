using System;
using System.Security.Claims;

namespace SCMM.Web.Server.API.Controllers.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid Id(this ClaimsPrincipal user)
        {
            Guid id;
            Guid.TryParse(user?.FindFirst(Domain.Models.Steam.ClaimTypes.Id)?.Value, out id);
            return id;
        }

        public static string SteamId(this ClaimsPrincipal user)
        {
            return user?.FindFirst(Domain.Models.Steam.ClaimTypes.SteamId)?.Value;
        }

        public static string Name(this ClaimsPrincipal user)
        {
            return user?.FindFirst(Domain.Models.Steam.ClaimTypes.Name)?.Value ?? user.Identity?.Name;
        }

        public static string Language(this ClaimsPrincipal user)
        {
            return user?.FindFirst(Domain.Models.Steam.ClaimTypes.Language)?.Value;
        }

        public static string Currency(this ClaimsPrincipal user)
        {
            return user?.FindFirst(Domain.Models.Steam.ClaimTypes.Currency)?.Value;
        }
    }
}
