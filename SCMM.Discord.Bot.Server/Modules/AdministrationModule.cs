using CommandQuery;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCMM.Discord.Bot.Server.Modules
{
    [Group("administration")]
    [Alias("admin")]
    [RequireOwner]
    [RequireContext(ContextType.DM)]
    public class AdministrationModule : ModuleBase<SocketCommandContext>
    {
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public AdministrationModule(SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

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

        [Command("asset import")]
        public async Task<RuntimeResult> AssetImportAsync(params ulong[] assetClassIds)
        {
            foreach (var assetClassId in assetClassIds)
            {
                _ = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
                {
                    AppId = Constants.RustAppId,
                    AssetClassId = assetClassId
                });
            }

            await _db.SaveChangesAsync();
            return CommandResult.Success();
        }

        [Command("asset collection create")]
        public async Task<RuntimeResult> AssetCollectionCreateAsync([Remainder] string collectionName)
        {
            var query = _db.SteamAssetDescriptions.Where(x => x.CreatorId != null);
            foreach (var word in collectionName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                query = query.Where(x => x.Name.Contains(word));
            }

            var assetDescriptions = await query.ToListAsync();
            foreach (var assetDescriptionGroup in assetDescriptions.GroupBy(x => x.CreatorId))
            {
                if (assetDescriptionGroup.Count() > 1)
                {
                    foreach (var assetDescription in assetDescriptionGroup)
                    {
                        assetDescription.ItemCollection = collectionName;
                    }
                }
            }

            await _db.SaveChangesAsync();
            return CommandResult.Success();
        }

        [Command("asset collection delete")]
        public async Task<RuntimeResult> AssetCollectionDeleteAsync([Remainder] string collectionName)
        {
            var assetDescriptions = await _db.SteamAssetDescriptions.Where(x => x.ItemCollection == collectionName).ToListAsync();
            foreach (var assetDescription in assetDescriptions)
            {
                assetDescription.ItemCollection = null;
            }

            await _db.SaveChangesAsync();
            return CommandResult.Success();
        }

        [Command("store name")]
        public async Task<RuntimeResult> StoreNameAsync(DateTime storeDate, [Remainder] string storeName)
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

        [Command("store media add")]
        public async Task<RuntimeResult> StoreMediaAddAsync(DateTime storeDate, [Remainder] string media)
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

        [Command("store media remove")]
        public async Task<RuntimeResult> StoreMediaRemoveAsync(DateTime storeDate, [Remainder] string media)
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
    }
}
