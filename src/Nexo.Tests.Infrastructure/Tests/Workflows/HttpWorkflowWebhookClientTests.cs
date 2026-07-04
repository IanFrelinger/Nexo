using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Nexo.Infrastructure.Workflows;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Workflows;

/// <summary>Tests for http workflow webhook client.</summary>
public class HttpWorkflowWebhookClientTests
{
    private static HttpWorkflowWebhookClient CreateClient(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler));
        /// <summary>Http workflow webhook client.</summary>
        return new HttpWorkflowWebhookClient(factory.Object, NullLogger<HttpWorkflowWebhookClient>.Instance);
    }

    [Fact]
    public async Task GetAsync_returns_response_body()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok-body", Encoding.UTF8, "text/plain"),
            });

        var client = CreateClient(handler.Object);
        var body = await client.GetAsync("https://example.com/hook");
        body.Should().Be("ok-body");
    }

    [Fact]
    public async Task PostAsync_sends_json_payload()
    {
        HttpRequestMessage? captured = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var client = CreateClient(handler.Object);
        await client.PostAsync("https://example.com/hook", new { name = "test" });
        captured.Should().NotBeNull();
        captured!.Method.Should().Be(HttpMethod.Post);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_rejects_blank_url(string? url)
    {
        var client = CreateClient(new Mock<HttpMessageHandler>().Object);
        var act = () => client.GetAsync(url!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Constructor_rejects_null_factory()
    {
        var act = () => new HttpWorkflowWebhookClient(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
