using Discord.Commands;
using SCMM.Web.Server.Domain;
using SCMM.Web.Shared;
using System;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Discord.Modules
{
	[Group("store")]
	public class StoreModule : ModuleBase<SocketCommandContext>
	{
		private readonly SteamService _steamService;

		public StoreModule(SteamService steamService)
		{
			_steamService = steamService;
		}

		/// <summary>
		///  !store remaining
		/// </summary>
		/// <returns></returns>
		[Command("remaining")]
		[Alias("next", "update", "expected")]
		[Summary("Echoes time remaining until the next expected store update")]
		public async Task SayStoreNextUpdateExpectedOnAsync()
        {
			var remainingTime = (_steamService.GetStoreNextUpdateExpectedOn() - DateTimeOffset.Now).ToDurationString(
				showDays: true,
				showHours: true,
				showMinutes: false,
				showSeconds: false
			);
			await ReplyAsync(
				$"Next store update is expected in about **{remainingTime}** from now"
			);
		}
	}
}
