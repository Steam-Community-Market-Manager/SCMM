using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;
using SCMM.Shared.API.Events;
using SCMM.Shared.Client;

namespace SCMM.Shared.Web.Handlers
{
    [Concurrency(MaxConcurrentCalls = 100)]
    public class WebProxyStatisticsUpdatedMessageHandler : IMessageHandler<WebProxyStatisticsUpdatedMessage>
    {
        private readonly ILogger<WebProxyStatisticsUpdatedMessageHandler> _logger;
        private readonly IWebProxyManager _webProxyManager;

        public WebProxyStatisticsUpdatedMessageHandler(ILogger<WebProxyStatisticsUpdatedMessageHandler> logger, IWebProxyManager webProxyManager)
        {
            _logger = logger;
            _webProxyManager = webProxyManager;
        }

        public async Task HandleAsync(WebProxyStatisticsUpdatedMessage message, IMessageContext context)
        {
            _logger.LogTrace($"Web proxy statistics have been updated, triggering refresh...");
            await _webProxyManager.RefreshProxiesAsync();
        }
    }
}
