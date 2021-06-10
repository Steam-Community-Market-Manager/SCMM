using Microsoft.AspNetCore.Authorization;
using SCMM.Shared.Data.Models;
using System;

namespace SCMM.Web.Server
{
    public static class AuthorizationPolicies
    {
        public const string Administrator = Roles.Administrator;
        public static Action<AuthorizationPolicyBuilder> AdministratorBuilder = (x) =>
        {
            x.RequireAuthenticatedUser();
            x.RequireRole(Roles.Administrator);
        };

        public const string User = Roles.User;
        public static Action<AuthorizationPolicyBuilder> UserBuilder = (x) =>
        {
            x.RequireAuthenticatedUser();
        };
    }
}
