using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using SCMM.Shared.API.Messages;

namespace SCMM.Web.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 100)]
    public class CurrencyExchangeRateUpdatedMessageHandler : IMessageHandler<CurrencyExchangeRateUpdatedMessage>
    {
        private readonly ILogger<CurrencyExchangeRateUpdatedMessageHandler> _logger;
        private readonly CurrencyCache _currencyCache;

        public CurrencyExchangeRateUpdatedMessageHandler(ILogger<CurrencyExchangeRateUpdatedMessageHandler> logger, CurrencyCache currencyCache)
        {
            _logger = logger;
            _currencyCache = currencyCache;
        }

        public Task HandleAsync(CurrencyExchangeRateUpdatedMessage message, MessageContext context)
        {
            _currencyCache.UpdateExchangeRate(message.Currency, message.ExchangeRateMultiplier);
            _logger.LogTrace($"Currency {message.Currency} exchange rate multiple has been updated to {message.ExchangeRateMultiplier}");
            return Task.CompletedTask;
        }
    }
}
