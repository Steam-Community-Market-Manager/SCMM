using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client.Commands;
using SCMM.Shared.API.Messages;
using SCMM.Shared.Data.Models;
using SCMM.Steam.API.Commands;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("roles")]
        [Alias("role")]
        public async Task<RuntimeResult> ListRoleAsync(string steamId)
        {
            var profile = await _steamDb.SteamProfiles
                .Where(x => x.SteamId == steamId || x.ProfileId == steamId)
                .FirstOrDefaultAsync();

            if (profile != null)
            {
                return CommandResult.Success(string.Join(", ", profile.Roles));
            }
            else
            {
                return CommandResult.Fail("Unable to find a profile");
            }
        }

        [Command("add-role")]
        public async Task<RuntimeResult> AddRoleAsync(string steamId, params string[] roles)
        {
            var profile = await _steamDb.SteamProfiles
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

                await _steamDb.SaveChangesAsync();
                return CommandResult.Success(string.Join(", ", profile.Roles));
            }
            else
            {
                return CommandResult.Fail("Unable to find a profile");
            }
        }

        [Command("remove-role")]
        public async Task<RuntimeResult> RemoveRoleAsync(string steamId, params string[] roles)
        {
            var profile = await _steamDb.SteamProfiles
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

                await _steamDb.SaveChangesAsync();
                return CommandResult.Success(string.Join(", ", profile.Roles));
            }
            else
            {
                return CommandResult.Fail("Unable to find a profile");
            }
        }

        [Command("donator")]
        public async Task<RuntimeResult> UpdateDonatorAsync(string steamId, [Remainder] int? donatorLevel = null)
        {
            var profile = await _steamDb.SteamProfiles
                .Where(x => x.SteamId == steamId || x.ProfileId == steamId)
                .FirstOrDefaultAsync();

            if (profile != null)
            {
                if (donatorLevel != null)
                {
                    profile.DonatorLevel = donatorLevel.Value;
                    await _steamDb.SaveChangesAsync();
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

        [Command("scan-bot-friends")]
        public async Task<RuntimeResult> ScanBotFriendsAsync()
        {
            var bots = await _steamDb.SteamProfiles
                .Where(x => x.Roles.Serialised.Contains(Roles.Bot))
                .Where(x => x.LastUpdatedFriendsOn == null)
                .OrderBy(x => x.LastUpdatedFriendsOn)
                .ToListAsync();

            var message = await Context.Channel.SendMessageAsync($"Checking bot friends...");
            foreach (var bot in bots)
            {
                await message.ModifyAsync(x => x.Content = $"Checking {bot.Name} ({bot.SteamId}) ({bots.IndexOf(bot) + 1}/{bots.Count})...");
                await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileFriendsRequest()
                {
                    ProfileId = bot.SteamId
                });
            }

            await message.ModifyAsync(x => x.Content = $"Checked {bots.Count} bot profiles");
            return CommandResult.Success();
        }

        [Command("scan-whale-friends")]
        public async Task<RuntimeResult> ScanWhaleFriendsAsync()
        {
            var bots = await _steamDb.SteamProfiles
                .Where(x => x.Roles.Serialised.Contains(Roles.Whale))
                .Where(x => x.LastUpdatedFriendsOn == null)
                .OrderBy(x => x.LastUpdatedFriendsOn)
                .ToListAsync();

            var message = await Context.Channel.SendMessageAsync($"Checking whale friends...");
            foreach (var bot in bots)
            {
                await message.ModifyAsync(x => x.Content = $"Checking {bot.Name} ({bot.SteamId}) ({bots.IndexOf(bot) + 1}/{bots.Count})...");
                await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileFriendsRequest()
                {
                    ProfileId = bot.SteamId
                });
            }

            await message.ModifyAsync(x => x.Content = $"Checked {bots.Count} whale profiles");
            return CommandResult.Success();
        }
    }
}
