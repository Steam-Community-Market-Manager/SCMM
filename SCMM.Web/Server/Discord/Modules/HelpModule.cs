using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using SCMM.Web.Server.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Discord.Modules
{
    [Group("help")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;
        private readonly ScmmDbContext _db;

        public HelpModule(IConfiguration configuration, ScmmDbContext db)
        {
            _configuration = configuration;
            _db = db;
        }

        /// <summary>
        /// !help
        /// </summary>
        /// <returns></returns>
        [Command]
        [Summary("Echo module help")]
        public async Task GetModuleHelpAsync()
        {
            var fields = new List<EmbedFieldBuilder>();
            fields.Add(new EmbedFieldBuilder()
                .WithName("`>config`")
                .WithValue("...")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`>store`")
                .WithValue("...")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`>inventory`")
                .WithValue("...")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`>trade`")
                .WithValue("...")
            );

            var embed = new EmbedBuilder()
                .WithTitle("Help")
                .WithDescription($"...")
                .WithFields(fields)
                .Build();

            await ReplyAsync(
                embed: embed
            );
        }
    }
}
