﻿using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
using System.Security.Claims;
using System.Text.RegularExpressions;
using ClaimTypes = SCMM.Shared.Data.Models.ClaimTypes;

namespace SCMM.Steam.API.Commands
{
    public class LoginSteamProfileRequest : ICommand<LoginSteamProfileResponse>
    {
        public string Claim { get; set; }
    }

    public class LoginSteamProfileResponse
    {
        public ClaimsIdentity Identity { get; set; }
    }

    public class LoginSteamProfile : ICommandHandler<LoginSteamProfileRequest, LoginSteamProfileResponse>
    {
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;

        public LoginSteamProfile(SteamDbContext db, ICommandProcessor commandProcessor)
        {
            _db = db;
            _commandProcessor = commandProcessor;
        }

        public async Task<LoginSteamProfileResponse> HandleAsync(LoginSteamProfileRequest request)
        {
            // Obtain the actual steam id from the login claim
            // e.g. https://steamcommunity.com/openid/id/<steamid>
            var steamId = Regex.Match(request.Claim, Constants.SteamLoginClaimSteamIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value;
            if (string.IsNullOrEmpty(steamId))
            {
                throw new ArgumentException(nameof(request), $"Unable to parse SteamID from '{steamId}'");
            }

            // Fetch the profile from steam
            var importedProfile = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
            {
                ProfileId = steamId,
                ImportFriendsListAsync = true,
                ImportInventoryAsync = true
            });

            // If this is an already existing profile
            var profile = importedProfile?.Profile;
            if (profile != null && !profile.IsTransient)
            {
                // Load more extended profile information from our database
                var profileInfoQuery = await _db.SteamProfiles
                    .Include(x => x.Language)
                    .Include(x => x.Currency)
                    .Where(x => x.Id == profile.Id)
                    .Select(x => new
                    {
                        Profile = x,
                        IsCreator = x.AssetDescriptions.Any(x => x.TimeAccepted != null),
                        IsDonator = x.DonatorLevel > 0
                    })
                    .FirstOrDefaultAsync();

                // Assign dynamic roles to the profile (if missing)
                var dynamicRoles = new List<string>();
                if (profileInfoQuery?.IsCreator == true)
                {
                    dynamicRoles.Add(Roles.Creator);
                }
                if (profileInfoQuery?.IsDonator == true)
                {
                    dynamicRoles.Add(Roles.Donator);
                    dynamicRoles.Add(Roles.VIP);
                }

                profile = (profileInfoQuery?.Profile ?? profile);
                if (dynamicRoles.Any())
                {
                    profile.Roles = new PersistableStringCollection(
                        profile.Roles?.Union(dynamicRoles)
                    );
                }
            }

            if (profile == null)
            {
                throw new ArgumentException(nameof(request), $"Unable to find Steam profile for '{steamId}'");
            }

            // Update the last signin timestamp
            profile.LastSignedInOn = DateTimeOffset.Now;

            // Build a identity for the profile
            var identity = new ClaimsIdentity(
                GetClaimsFromSteamProfile(profile),
                "SCMM",
                ClaimTypes.Name,
                ClaimTypes.Role
            );

            return new LoginSteamProfileResponse()
            {
                Identity = identity
            };
        }

        private IEnumerable<Claim> GetClaimsFromSteamProfile(SteamProfile profile)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Id, profile.Id.ToString())
            };
            if (!string.IsNullOrEmpty(profile.SteamId))
            {
                claims.Add(new Claim(ClaimTypes.SteamId, profile.SteamId));
            }
            if (!string.IsNullOrEmpty(profile.ProfileId))
            {
                claims.Add(new Claim(ClaimTypes.ProfileId, profile.ProfileId));
            }
            if (!string.IsNullOrEmpty(profile.Name))
            {
                claims.Add(new Claim(ClaimTypes.Name, profile.Name));
            }
            if (!string.IsNullOrEmpty(profile.AvatarUrl))
            {
                claims.Add(new Claim(ClaimTypes.AvatarUrl, profile.AvatarUrl));
            }
            if (!string.IsNullOrEmpty(profile.AvatarLargeUrl))
            {
                claims.Add(new Claim(ClaimTypes.AvatarLargeUrl, profile.AvatarLargeUrl)); ;
            }
            if (profile.Language != null)
            {
                claims.Add(new Claim(ClaimTypes.Language, profile.Language.Name));
            }
            if (profile.Currency != null)
            {
                claims.Add(new Claim(ClaimTypes.Currency, profile.Currency.Name));
            }
            foreach (var role in profile.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            return claims;
        }
    }
}
