using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Nexo.Infrastructure.Workflows;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Workflows;

public sealed class HttpWorkflowWebhookClientGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_factory()
    {
        var act = () => new HttpWorkflowWebhookClient(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClientFactory");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_rejects_blank_url(string? url)
    {
        var client = CreateClient(new Mock<HttpMessageHandler>().Object);

        var act = () => client.GetAsync(url!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("url");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PostAsync_rejects_blank_url(string? url)
    {
        var client = CreateClient(new Mock<HttpMessageHandler>().Object);

        var act = () => client.PostAsync(url!, new { ok = true });

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("url");
    }

    [Fact]
    public async Task GetAsync_throws_when_response_is_not_successful()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadGateway));

        var client = CreateClient(handler.Object);

        var act = () => client.GetAsync("https://example.com/hook");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task PostAsync_throws_when_response_is_not_successful()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = CreateClient(handler.Object);

        var act = () => client.PostAsync("https://example.com/hook", new { ok = true });

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task PostAsync_succeeds_for_valid_response()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var client = CreateClient(handler.Object);

        var act = () => client.PostAsync("https://example.com/hook", new { ok = true });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAsync_uses_factory_created_client()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("payload", Encoding.UTF8, "text/plain"),
            });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler.Object));
        var client = new HttpWorkflowWebhookClient(factory.Object, NullLogger<HttpWorkflowWebhookClient>.Instance);

        var body = await client.GetAsync("https://example.com/hook");

        body.Should().Be("payload");
        factory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Once);
    }

    private static HttpWorkflowWebhookClient CreateClient(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler));
        return new HttpWorkflowWebhookClient(factory.Object, NullLogger<HttpWorkflowWebhookClient>.Instance);
    }
}
