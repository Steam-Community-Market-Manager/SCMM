using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("donator")]
        public async Task<RuntimeResult> UpdateDonatorAsync(string steamId, [Remainder] int? donatorLevel)
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
                    $"Donator level is {profile.DonatorLevel}"
                );
            }
            else
            {
                return CommandResult.Fail("Unable to find a profile");
            }
        }
    }
}
