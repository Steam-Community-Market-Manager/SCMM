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
        public async Task<RuntimeResult> ProfileDonatorAsync(string steamId, [Remainder] int donatorLevel)
        {
            var profile = await _db.SteamProfiles
                .Where(x => x.SteamId == steamId)
                .FirstOrDefaultAsync();

            if (profile != null)
            {
                profile.DonatorLevel = donatorLevel;
                await _db.SaveChangesAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Fail("Unable to find a profile for the SteamID");
            }
        }
    }
}
