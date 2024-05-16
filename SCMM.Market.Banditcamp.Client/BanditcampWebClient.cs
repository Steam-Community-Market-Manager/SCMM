using Microsoft.Extensions.Logging;
using SocketIO.Core;
using SocketIOClient;
using SocketIOClient.Transport;

namespace SCMM.Market.Banditcamp.Client
{
    public class BanditcampWebClient
    {
        private ILogger<BanditcampWebClient> _logger;

        private const string WebsiteBaseHostname = "bandit.camp";
        private const string ApiBaseHostname = "api.bandit.camp";

        public BanditcampWebClient(ILogger<BanditcampWebClient> logger)
        {
            _logger = logger;
        }

        private async Task<SocketIOClient.SocketIO> ConnectClientAsync(Func<SocketIOClient.SocketIO, Task> onConnectedAsync)
        {
            var client = new SocketIOClient.SocketIO($"wss://{ApiBaseHostname}", new SocketIOOptions
            {
                Path = "",
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
                    "connect", 
                    new { 
                        isTrusted = true 
                    }
                );
            };

            client.On("connected", async response =>
            {
                await onConnectedAsync(client);
            });

            // Connect and monitor events
            await client.ConnectAsync();
            return client;
        }

        public async Task<IEnumerable<BanditcampSiteInventoryItem>> GetSiteInventory()
        {
            var inventory = new List<BanditcampSiteInventoryItem>();
            var inventoryWasLoadedEvent = new ManualResetEvent(false);
            using var client = await ConnectClientAsync(async c =>
            {
                await c.EmitAsync(
                    "user.inventory.buy.listings",
                    (ack) => {
                        try
                        {
                            var items = ack.GetValue<BanditcampSiteInventoryResponse>();
                            if (items?.Any() == true)
                            {
                                inventory.AddRange(items);
                            }
                        }
                        finally
                        {
                            inventoryWasLoadedEvent.Set();
                        }
                    },
                    new BanditcampSiteInventoryRequest()
                );
            });

            inventoryWasLoadedEvent.WaitOne(TimeSpan.FromMinutes(3));
            return inventory;
        }
    }
}
