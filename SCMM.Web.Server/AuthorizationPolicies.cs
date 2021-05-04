using AspNet.Security.OpenId.Steam;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using System;

namespace SCMM.Web.Server
{
    public static class AuthorizationPolicies
    {
        public const string Administrator = "Administrator";
        public static Action<AuthorizationPolicyBuilder> AdministratorBuilder = (x) =>
        {
            x.AddAuthenticationSchemes(SteamAuthenticationDefaults.AuthenticationScheme);
            x.RequireAuthenticatedUser();
            x.RequireRole("Administrator");
        };

        public const string User = "User";
        public static Action<AuthorizationPolicyBuilder> UserBuilder = (x) =>
        {
            x.AddAuthenticationSchemes(SteamAuthenticationDefaults.AuthenticationScheme);
            x.RequireAuthenticatedUser();
        };
    }
}
