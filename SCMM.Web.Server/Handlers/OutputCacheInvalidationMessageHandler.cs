using Microsoft.AspNetCore.OutputCaching;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;
using SCMM.Shared.API.Events;

namespace SCMM.Web.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 100)]
    public class OutputCacheInvalidationMessageHandler : 
        IMessageHandler<AppItemDefinitionsUpdatedMessage>,
        IMessageHandler<CurrencyExchangeRateUpdatedMessage>,
        IMessageHandler<ItemDefinitionAddedMessage>,
        IMessageHandler<MarketItemAddedMessage>,
        IMessageHandler<StoreAddedMessage>,
        IMessageHandler<StoreItemAddedMessage>
    {
        private readonly ILogger<OutputCacheInvalidationMessageHandler> _logger;
        private readonly IOutputCacheStore _outputCacheStore;

        public OutputCacheInvalidationMessageHandler(ILogger<OutputCacheInvalidationMessageHandler> logger, IOutputCacheStore outputCacheStore)
        {
            _logger = logger;
            _outputCacheStore = outputCacheStore;
        }
        public async Task HandleAsync(AppItemDefinitionsUpdatedMessage message, IMessageContext context)
        {
            await _outputCacheStore.EvictByTagAsync(CacheTag.ItemDefinition, default);
        }

        public async Task HandleAsync(CurrencyExchangeRateUpdatedMessage message, IMessageContext context)
        {
            await _outputCacheStore.EvictByTagAsync(CacheTag.Currency, default);
        }

        public async Task HandleAsync(ItemDefinitionAddedMessage message, IMessageContext context)
        {
            await _outputCacheStore.EvictByTagAsync(CacheTag.Item, default);
        }

        public async Task HandleAsync(MarketItemAddedMessage message, IMessageContext context)
        {
            await _outputCacheStore.EvictByTagAsync(CacheTag.Market, default);
            await _outputCacheStore.EvictByTagAsync(CacheTag.Item, default);
        }

        public async Task HandleAsync(StoreAddedMessage message, IMessageContext context)
        {
            await _outputCacheStore.EvictByTagAsync(CacheTag.Store, default);
        }

        public async Task HandleAsync(StoreItemAddedMessage message, IMessageContext context)
        {
            await _outputCacheStore.EvictByTagAsync(CacheTag.Store, default);
            await _outputCacheStore.EvictByTagAsync(CacheTag.Item, default);
        }
    }
}
