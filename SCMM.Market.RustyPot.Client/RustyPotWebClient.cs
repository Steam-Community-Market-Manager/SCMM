using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.API.Messages;
using SocketIOClient;
using SocketIOClient.Transport;

namespace SCMM.Market.RustyPot.Client
{
    public class RustyPotWebClient : Shared.Client.WebClient
    {
        private ILogger<RustyPotWebClient> _logger;
        private readonly IServiceBus _serviceBus;
        private readonly ICollection<string> _steamIds;

        private const string WebsiteHostname = "rustypot.com";

        public RustyPotWebClient(ILogger<RustyPotWebClient> logger, IServiceBus serviceBus)
        {
            _logger = logger;
            _serviceBus = serviceBus;
            _steamIds = new HashSet<string>();

            DefaultHeaders.Add("Host", WebsiteHostname);
            DefaultHeaders.Add("Origin", $"http://{WebsiteHostname}");
            DefaultHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Mobile Safari/537.36");
        }

        public async Task<IDisposable> MonitorAsync()
        {
            var client = new SocketIO($"wss://{WebsiteHostname}", new SocketIOOptions
            {
                Path = "/socket.io",
                EIO = EngineIO.V4,
                Transport = TransportProtocol.WebSocket,
                ExtraHeaders = DefaultHeaders.ToDictionary(x => x.Key, x => x.Value)
            });

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("jackpot get deposits");
                await client.EmitAsync("get jackpotGameHistory");
                await client.EmitAsync("get jackpotGameHash");
                await client.EmitAsync("get cfLobbys");
                await client.EmitAsync("get cf history");
                await client.EmitAsync("get chatHistory");
            };

            //
            // JACKPOTS
            //

            client.On("return jackpot deposit", async response =>
            {
                var jackpotDeposits = response.GetValue<RustyPotJackpotDeposit[]>();
                if (jackpotDeposits?.Any() == true)
                {
                    await ImportProfiles(
                        jackpotDeposits.Select(x => x.ProfileId).ToArray()
                    );
                }
            });
            client.On("new BiggestBet", async response =>
            {
                var jackpotBiggestBet = response.GetValue<RustyPotJackpotBiggestBet>();
                if (jackpotBiggestBet != null)
                {
                    await ImportProfiles(jackpotBiggestBet.ProfileId);
                }
            });

            //
            // COINFLIPS
            //

            client.On("return allActiveCoinflips", async response =>
            {
                var coinflips = response.GetValue<IDictionary<string, RustyPotCoinflip>>();
                if (coinflips?.Any() == true)
                {
                    await ImportProfiles(coinflips
                        .SelectMany(x => new[]
                        {
                            x.Value?.Creator?.ProfileId,
                            x.Value?.Opponent?.ProfileId,
                            x.Value?.Winner?.ProfileId,
                            x.Value?.BotProfileId
                        })
                        .ToArray()
                    );
                }
            });
            client.On("cf newLobby", async response =>
            {
                var coinflip = response.GetValue<RustyPotCoinflip>();
                if (coinflip != null)
                {
                    await ImportProfiles(
                        coinflip.Creator?.ProfileId,
                        coinflip.Opponent?.ProfileId,
                        coinflip?.Winner?.ProfileId,
                        coinflip.BotProfileId
                    );
                }
            });
            client.On("updateCFStatus", async response =>
            {
                var coinflip = response.GetValue<RustyPotCoinflip>();
                if (coinflip != null)
                {
                    await ImportProfiles(
                        coinflip.Creator?.ProfileId,
                        coinflip.Opponent?.ProfileId,
                        coinflip?.Winner?.ProfileId,
                        coinflip.BotProfileId
                    );
                }
            });

            //
            // CHAT
            //

            client.On("return chatHistory", async response =>
            {
                var chatMessages = response.GetValue<RustyPotChatMessage[]>();
                if (chatMessages?.Any() == true)
                {
                    await ImportProfiles(
                        chatMessages.Select(x => x.ProfileId).ToArray()
                    );
                }
            });
            client.On("new Chat", async response =>
            {
                var chatMessage = response.GetValue<RustyPotChatMessage>();
                if (chatMessage != null)
                {
                    await ImportProfiles(chatMessage.ProfileId);
                }
            });

            // Connect and monitor events
            await client.ConnectAsync();
            return client;
        }

        private async Task ImportProfiles(params string[] steamIds)
        {
            var importProfileTasks = new List<ImportProfileMessage>();
            lock (_steamIds)
            {
                foreach (var steamId in steamIds.Distinct())
                {
                    if (string.IsNullOrEmpty(steamId) || string.Equals(steamId, "null", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (!_steamIds.Contains(steamId))
                    {
                        _steamIds.Add(steamId);
                        importProfileTasks.Add(new ImportProfileMessage()
                        {
                            ProfileId = steamId
                        });
                    }
                }
            }
            if (importProfileTasks.Any())
            {
                await _serviceBus.SendMessagesAsync(importProfileTasks);
            }
        }
    }
}
