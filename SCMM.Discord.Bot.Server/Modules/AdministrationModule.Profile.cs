using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("roles")]
        [Alias("role")]
        public async Task<RuntimeResult> ListRoleAsync(string steamId)
        {
            var profile = await _db.SteamProfiles
                .Where(x => x.SteamId == steamId || x.ProfileId == steamId)
                .FirstOrDefaultAsync();

            if (profile != null)
            {
                return CommandResult.Success(String.Join(", ", profile.Roles));
            }
            else
            {
                return CommandResult.Fail("Unable to find a profile");
            }
        }

        [Command("role-add")]
        public async Task<RuntimeResult> AddRoleAsync(string steamId, params string[] roles)
        {
            var profile = await _db.SteamProfiles
                .Where(x => x.SteamId == steamId || x.ProfileId == steamId)
                .FirstOrDefaultAsync();

            if (profile != null)
            {
                foreach (var role in roles)
                {
                    if (!profile.Roles.Contains(role))
                    {
                        profile.Roles.Add(role);
                    }
                }

                await _db.SaveChangesAsync();
                return CommandResult.Success(String.Join(", ", profile.Roles));
            }
            else
            {
                return CommandResult.Fail("Unable to find a profile");
            }
        }

        [Command("role-remove")]
        public async Task<RuntimeResult> RemoveRoleAsync(string steamId, params string[] roles)
        {
            var profile = await _db.SteamProfiles
                .Where(x => x.SteamId == steamId || x.ProfileId == steamId)
                .FirstOrDefaultAsync();

            if (profile != null)
            {
                foreach (var role in roles)
                {
                    if (profile.Roles.Contains(role))
                    {
                        profile.Roles.Remove(role);
                    }
                }

                await _db.SaveChangesAsync();
                return CommandResult.Success(String.Join(", ", profile.Roles));
            }
            else
            {
                return CommandResult.Fail("Unable to find a profile");
            }
        }

        [Command("donator")]
        public async Task<RuntimeResult> UpdateDonatorAsync(string steamId, [Remainder] int? donatorLevel = null)
        {
            var profile = await _db.SteamProfiles
                .Where(x => x.SteamId == steamId || x.ProfileId == steamId)
                .FirstOrDefaultAsync();

            if (profile != null)
            {
                if (donatorLevel != null)
                {
                    profile.DonatorLevel = donatorLevel.Value;
                    await _db.SaveChangesAsync();
                }

                return CommandResult.Success(
                    $"Donator level is **{profile.DonatorLevel}**"
                );
            }
            else
            {
                return CommandResult.Fail("Unable to find a profile");
            }
        }
    }
}
