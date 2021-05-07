using CommandQuery;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using SCMM.Steam.Data.Store;

namespace SCMM.Discord.Bot.Server.Modules
{
    [Group("administration")]
    [Alias("admin")]
    [RequireOwner]
    [RequireContext(ContextType.DM)]
    public class AdministrationModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public AdministrationModule(IConfiguration configuration, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _configuration = configuration;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }
    }
}
