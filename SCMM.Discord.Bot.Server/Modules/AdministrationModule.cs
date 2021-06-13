using CommandQuery;
using Discord.Commands;
using SCMM.Steam.Data.Store;
using System.Text.RegularExpressions;

namespace SCMM.Discord.Bot.Server.Modules
{
    [Group("administration")]
    [Alias("admin")]
    [RequireOwner]
    [RequireContext(ContextType.DM)]
    public partial class AdministrationModule : ModuleBase<SocketCommandContext>
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
    }
}
