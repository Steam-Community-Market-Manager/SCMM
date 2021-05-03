using SCMM.Shared.Azure.ServiceBus;
using SCMM.Shared.Azure.ServiceBus.Attributes;
using SCMM.Steam.API.Messages;
using SCMM.Web.Server;
using System.Threading.Tasks;
using SCMM.Discord.Bot.Server.Handlers;

namespace SCMM.Web.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 100)]
    public class CurrencyExchangeRateUpdateMessageHandler : IMessageHandler<CurrencyExchangeRateUpdateMessage>
    {
        private readonly CurrencyCache _currencyCache;

        public CurrencyExchangeRateUpdateMessageHandler(CurrencyCache currencyCache)
        {
            _currencyCache = currencyCache;
        }

        public Task HandleAsync(CurrencyExchangeRateUpdateMessage message)
        {
            _currencyCache.UpdateExchangeRate(message.Currency, message.ExchangeRateMultiplier);
            return Task.CompletedTask;
        }
    }
}
