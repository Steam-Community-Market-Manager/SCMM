using Microsoft.Extensions.Logging;
using SocketIO.Core;
using SocketIOClient;
using SocketIOClient.Transport;

namespace SCMM.Market.Rustyloot.Client
{
    public class RustylootWebClient
    {
        private ILogger<RustylootWebClient> _logger;

        private const string WebsiteBaseHostname = "rustyloot.gg";
        private const string ApiBaseHostname = "api.rustyloot.gg";

        public RustylootWebClient(ILogger<RustylootWebClient> logger)
        {
            _logger = logger;
        }

        private async Task<SocketIOClient.SocketIO> ConnectClientAsync(Func<SocketIOClient.SocketIO, Task> onConnectedAsync)
        {
            var client = new SocketIOClient.SocketIO($"wss://{ApiBaseHostname}", new SocketIOOptions
            {
                Path = "/socket.io",
                EIO = EngineIO.V4,
                Transport = TransportProtocol.WebSocket,
                ExtraHeaders = new Dictionary<string, string>
                {
                    { "Host", ApiBaseHostname },
                    { "Origin", $"https://{WebsiteBaseHostname}" },
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36" }
                }
            });

            client.OnError += (sender, e) =>
            {
                _logger.LogError(e);
            };

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync(
                    "system:connect", 
                    new { }
                );
            };

            client.On("system:connect", async response =>
            {
                await onConnectedAsync(client);
            });

            // Connect and monitor events
            await client.ConnectAsync();
            return client;
        }

        public async Task<IEnumerable<RustylootSteamMarketInventoryItem>> GetSiteInventory()
        {
            var inventory = new List<RustylootSteamMarketInventoryItem>();
            var inventoryWasLoadedEvent = new ManualResetEvent(false);
            using var client = await ConnectClientAsync(async c =>
            {
                await c.EmitAsync(
                    "steam:market",
                    (ack) => {
                        try
                        {
                            var steamMarket = ack.GetValue<RustylootSteamMarketResponse>();
                            if (!steamMarket.Error)
                            {
                                if (steamMarket?.Data?.Inventory?.Any() == true)
                                {
                                    inventory.AddRange(steamMarket.Data.Inventory);
                                }
                            }
                        }
                        finally
                        {
                            inventoryWasLoadedEvent.Set();
                        }
                    },
                    new RustylootSteamMarketRequest()
                );
            });

            inventoryWasLoadedEvent.WaitOne(TimeSpan.FromMinutes(3));
            return inventory;
        }
    }
}
