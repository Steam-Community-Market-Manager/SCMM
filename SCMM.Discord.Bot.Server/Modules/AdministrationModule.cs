using CommandQuery;
using Discord.Commands;
using SCMM.Azure.ServiceBus;
using SCMM.Fixer.Client;
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
        private readonly ServiceBusClient _serviceBusClient;
        private readonly SteamWebClient _steamWebClient;
        private readonly FixerWebClient _fixerWebClient;
        private readonly GoogleClient _googleClient;

        public AdministrationModule(SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, ServiceBusClient serviceBusClient, SteamWebClient steamWebClient, FixerWebClient fixerWebClient, GoogleClient googleClient)
        {
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _serviceBusClient = serviceBusClient;
            _steamWebClient = steamWebClient;
            _fixerWebClient = fixerWebClient;
            _googleClient = googleClient;
        }
    }
}
