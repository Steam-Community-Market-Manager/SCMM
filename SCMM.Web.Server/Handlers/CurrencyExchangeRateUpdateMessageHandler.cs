﻿using Microsoft.Extensions.Logging;
using SCMM.Shared.Azure.ServiceBus;
using SCMM.Shared.Azure.ServiceBus.Attributes;
using SCMM.Steam.API.Messages;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 100)]
    public class CurrencyExchangeRateUpdateMessageHandler : IMessageHandler<CurrencyExchangeRateUpdateMessage>
    {
        private readonly ILogger<CurrencyExchangeRateUpdateMessageHandler> _logger;
        private readonly CurrencyCache _currencyCache;

        public CurrencyExchangeRateUpdateMessageHandler(ILogger<CurrencyExchangeRateUpdateMessageHandler> logger, CurrencyCache currencyCache)
        {
            _logger = logger;
            _currencyCache = currencyCache;
        }

        public Task HandleAsync(CurrencyExchangeRateUpdateMessage message)
        {
            _currencyCache.UpdateExchangeRate(message.Currency, message.ExchangeRateMultiplier);
            _logger.LogInformation($"Currency {message.Currency} exchange rate multiple has been updated to {message.ExchangeRateMultiplier}");
            return Task.CompletedTask;
        }
    }
}