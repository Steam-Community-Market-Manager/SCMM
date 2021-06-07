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

        [Command("asset import")]
        public async Task<RuntimeResult> AssetImportAsync(params ulong[] assetIds)
        {
            foreach (var assetId in assetIds)
            {
                _ = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
                {
                    AppId = 252490, // Rust
                    AssetId = assetId
                });

                await _db.SaveChangesAsync();
            }

            return CommandResult.Success();
        }

        [Command("asset tag set")]
        public async Task<RuntimeResult> AssetTagSetAsync(string set)
        {
            var query = _db.SteamAssetDescriptions.AsQueryable();
            foreach (var word in set.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                query = query.Where(x => x.Name.Contains(word));
            }

            var assets = await query.ToListAsync();
            if (assets.Any())
            {
                foreach (var asset in assets)
                {
                    asset.Tags[Constants.SteamAssetTagSet] = set;
                }

                await _db.SaveChangesAsync();
                return CommandResult.Success($"{assets.Count} assets were updated. {String.Join(", ", assets.Select(x => x.Name))}.");
            }
            else
            {
                return CommandResult.Fail($"No assets found matching \"{set}\"");
            }
        }
    }
}
