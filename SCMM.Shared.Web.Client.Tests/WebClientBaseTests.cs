using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq.Protected;
using Polly;
using System.Net;

namespace SCMM.Shared.Web.Client.Tests;

public class WebClientBaseTests
{
    private const string GetRequestUri = "https://github.com/Steam-Community-Market-Manager/SCMM";

    [Fact]
    public async Task HttpClientGetReturnASuccessStatusCode()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<TestWebClient>();
        var webClientFactory = new TestWebClient(logger);
        using (var httpClient = webClientFactory.BuildHttpClient())
        {
            // Act
            var httpResponseMessage = await webClientFactory.RetryPolicy.ExecuteAsync(
                () => httpClient.GetAsync(GetRequestUri)
            );

            // Assert
            Assert.True(httpResponseMessage.IsSuccessStatusCode);
        }
    }

    [Fact]
    public async Task HttpClientGetReturnASuccessStatusCodeAfterAnInitialUnsuccessfulRequest()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<TestWebClient>();
        var webClientFactory = new TestWebClient(logger, httpHandler: BuildMockHttpHandlerFailOnFirstRequestThenSucceedOnSecondRequest().Object);
        using (var httpClient = webClientFactory.BuildHttpClient())
        {
            // Act
            var httpResponseMessage = await webClientFactory.RetryPolicy.ExecuteAsync(
                () => httpClient.GetAsync(GetRequestUri)
            );

            // Assert
            Assert.True(httpResponseMessage.IsSuccessStatusCode);
        }
    }

    private static Mock<HttpMessageHandler> BuildMockHttpHandlerFailOnFirstRequestThenSucceedOnSecondRequest(HttpStatusCode failureStatusCode = HttpStatusCode.InternalServerError)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
           .Protected()
           .SetupSequence<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
            )
           .ReturnsAsync(() => new HttpResponseMessage
           {
               StatusCode = failureStatusCode
           })
           .ReturnsAsync(() => new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.Accepted
           });

        return handlerMock;
    }

    internal class TestWebClient : WebClientBase
    {
        public TestWebClient(ILogger logger, HttpMessageHandler httpHandler = null) : base(logger, httpHandler)
        {
        }

        public new HttpClient BuildHttpClient()
        {
            return base.BuildHttpClient();
        }

        public new AsyncPolicy<HttpResponseMessage> RetryPolicy => base.RetryPolicy;
    }
}