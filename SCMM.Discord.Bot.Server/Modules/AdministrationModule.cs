using CommandQuery;
using Discord.Commands;
using SCMM.Google.Client;
using SCMM.Steam.Client;
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
        private readonly SteamWebClient _steamWebClient;
        private readonly GoogleClient _googleClient;

        public AdministrationModule(SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, SteamWebClient steamWebClient, GoogleClient googleClient)
        {
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _steamWebClient = steamWebClient;
            _googleClient = googleClient;
        }
    }
}
