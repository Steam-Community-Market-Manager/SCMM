using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client;
using SCMM.Shared.Data.Models.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("update-store-name")]
        public async Task<RuntimeResult> UpdateStoreNameAsync(DateTime storeDate, [Remainder] string storeName)
        {
            var itemStore = await _db.SteamItemStores
                .OrderByDescending(x => x.Start)
                .Where(x => storeDate >= x.Start)
                .FirstOrDefaultAsync();

            if (itemStore != null)
            {
                itemStore.Name = storeName;
                await _db.SaveChangesAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Fail("Unable to find a store for the specific date");
            }
        }

        [Command("add-store-media")]
        public async Task<RuntimeResult> AddStoreMediaAsync(DateTime storeDate, [Remainder] string media)
        {
            var itemStore = await _db.SteamItemStores
                .OrderByDescending(x => x.Start)
                .Where(x => storeDate >= x.Start)
                .FirstOrDefaultAsync();

            if (itemStore != null)
            {
                itemStore.Media.Add(media);
                await _db.SaveChangesAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Fail("Unable to find a store for the specific date");
            }
        }

        [Command("remove-store-media")]
        public async Task<RuntimeResult> RemoveStoreMediaAsync(DateTime storeDate, [Remainder] string media)
        {
            var itemStore = await _db.SteamItemStores
                .OrderByDescending(x => x.Start)
                .Where(x => storeDate >= x.Start)
                .FirstOrDefaultAsync();

            if (itemStore != null)
            {
                itemStore.Media.Remove(media);
                await _db.SaveChangesAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Fail("Unable to find a store for the specific date");
            }
        }

        [Command("rebuild-store-list")]
        public async Task<RuntimeResult> RebuildStoreListAsync()
        {
            //...

            return CommandResult.Success();
        }
    }
}
