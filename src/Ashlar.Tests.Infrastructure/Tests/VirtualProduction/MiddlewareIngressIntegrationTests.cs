using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Ashlar.API.Middleware.Ingress;
using Ashlar.Contracts;
using Ashlar.Tests.Infrastructure.Helpers.VirtualProduction;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.VirtualProduction;

/// <summary>Tests for middleware ingress integration.</summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class MiddlewareIngressIntegrationTests : IClassFixture<AshlarApiWebApplicationFactory>
{
    private readonly AshlarApiWebApplicationFactory _factory;

    /// <summary>Middleware ingress integration tests.</summary>
    /// <param name="factory">Factory.</param>
    public MiddlewareIngressIntegrationTests(AshlarApiWebApplicationFactory factory) => _factory = factory;

    [Fact(Timeout = 60000)]
    public async Task Correlation_id_round_trips_on_http_and_echo_endpoint()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "corr-test-1");
        using var resp = await client.GetAsync(new Uri("api/middleware/correlation-echo", UriKind.Relative));
        resp.EnsureSuccessStatusCode();
        resp.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var hdrs);
        hdrs.Should().ContainSingle().Which.Should().Be("corr-test-1");
        var doc = await resp.Content.ReadFromJsonAsync<JsonElement>();
        doc.GetProperty("correlationId").GetString().Should().Be("corr-test-1");
    }

    [Fact(Timeout = 60000)]
    public async Task Ingress_context_reflects_optional_headers()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "corr-ctx-1");
        client.DefaultRequestHeaders.Add("X-Ashlar-Tenant", "tenant-x");
        client.DefaultRequestHeaders.Add("X-Ashlar-App-Id", "app-y");
        client.DefaultRequestHeaders.Add("X-Idempotency-Key", "idem-z");
        client.DefaultRequestHeaders.Add("X-Ashlar-Payload-Version", "v2");
        using var resp = await client.GetAsync(new Uri("api/middleware/ingress-context", UriKind.Relative));
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<IngressContextResponse>();
        dto.Should().NotBeNull();
        dto!.CorrelationId.Should().Be("corr-ctx-1");
        dto.Transport.Should().Be(AshlarIngressTransports.Http);
        dto.TenantId.Should().Be("tenant-x");
        dto.AppId.Should().Be("app-y");
        dto.IdempotencyKey.Should().Be("idem-z");
        dto.PayloadVersion.Should().Be("v2");
    }

    [Fact(Timeout = 60000)]
    public async Task Swagger_document_lists_middleware_ingress_operations()
    {
        var client = _factory.CreateClient();
        using var resp = await client.GetAsync(new Uri("swagger/v1/swagger.json", UriKind.Relative));
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("IngressCorrelationEcho");
        json.Should().Contain("SmsInboundSimulate");
        json.Should().Contain("AwsSnsSmsWebhook");
        json.Should().Contain("IngressContext");
    }

    [Fact(Timeout = 60000)]
    public async Task Sms_simulation_accepts_yes_token_and_replays_idempotently()
    {
        var client = _factory.CreateClient();
        var body = new SmsInboundSimulationRequest("+15555550100", "YES approve-z", MessageSid: "SM123");
        using var resp = await client.PostAsJsonAsync(new Uri("api/ingress/sms/simulate", UriKind.Relative), body);
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<SmsInboundSimulationResponse>();
        dto.Should().NotBeNull();
        dto!.Accepted.Should().BeTrue();
        dto.ApprovalToken.Should().Be("approve-z");
        dto.IdempotentReplay.Should().BeFalse();

        using var resp2 = await client.PostAsJsonAsync(new Uri("api/ingress/sms/simulate", UriKind.Relative), body);
        resp2.EnsureSuccessStatusCode();
        var dto2 = await resp2.Content.ReadFromJsonAsync<SmsInboundSimulationResponse>();
        dto2!.IdempotentReplay.Should().BeTrue();
    }

    [Fact(Timeout = 60000)]
    public async Task Aws_sns_webhook_accepts_notification_with_signature_skip_in_testing()
    {
        var client = _factory.CreateClient();
        const string snsBody = """
            {
              "Type": "Notification",
              "MessageId": "sns-itest-1",
              "TopicArn": "arn:aws:sns:us-east-1:123456789012:test",
              "Message": "YES sns-yes-1",
              "Timestamp": "2024-01-01T00:00:00.000Z",
              "SignatureVersion": "2",
              "Signature": "dGVzdA==",
              "SigningCertURL": "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-nonexistent.pem"
            }
            """;
        using var content = new StringContent(snsBody, Encoding.UTF8, "application/json");
        using var resp = await client.PostAsync(new Uri("api/ingress/sms/sns", UriKind.Relative), content);
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<SmsInboundSimulationResponse>();
        dto.Should().NotBeNull();
        dto!.Accepted.Should().BeTrue();
        dto.ApprovalToken.Should().Be("sns-yes-1");
        dto.IdempotentReplay.Should().BeFalse();

        using var content2 = new StringContent(snsBody, Encoding.UTF8, "application/json");
        using var resp2 = await client.PostAsync(new Uri("api/ingress/sms/sns", UriKind.Relative), content2);
        resp2.EnsureSuccessStatusCode();
        var dto2 = await resp2.Content.ReadFromJsonAsync<SmsInboundSimulationResponse>();
        dto2!.IdempotentReplay.Should().BeTrue();
    }

    [Fact(Timeout = 60000)]
    public async Task WebSocket_echo_returns_hello_and_echo()
    {
        var server = (TestServer)_factory.Server;
        var wsClient = server.CreateWebSocketClient();
        var uri = new UriBuilder(server.BaseAddress!) { Scheme = "ws", Path = "ws/v1/echo" }.Uri;
        using var ws = await wsClient.ConnectAsync(uri, CancellationToken.None);
        var buf = new byte[8192];
        var r1 = await ws.ReceiveAsync(buf.AsMemory(), CancellationToken.None);
        r1.MessageType.Should().Be(WebSocketMessageType.Text);
        var hello = Encoding.UTF8.GetString(buf.AsSpan(0, r1.Count));
        hello.Should().Contain("hello");

        var send = Encoding.UTF8.GetBytes("{\"ping\":true}");
        await ws.SendAsync(send.AsMemory(), WebSocketMessageType.Text, true, CancellationToken.None);
        var r2 = await ws.ReceiveAsync(buf.AsMemory(), CancellationToken.None);
        var echo = Encoding.UTF8.GetString(buf.AsSpan(0, r2.Count));
        echo.Should().Contain("echo");
        echo.Should().Contain("ping");
    }
}
