using System.Net;
using FluentAssertions;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Transport;
using Nexo.Runtime.Barriers;
using Nexo.Transport.Grpc;
using Nexo.Transport.Grpc.Server;
using Xunit;

namespace Nexo.Tests.Transport;

[Collection("GrpcTransportEnvironment")]
public sealed class AgentTransportServiceImplGapCoverageTests
{
    [Fact]
    public async Task Invoke_returns_barrier_validation_failure_when_explicit_barrier_required()
    {
        await using var fixture = await GrpcTransportTestFixture.StartAsync(
            new EchoTransport(),
            requireExplicitBarrier: true);

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);
        using var transport = new GrpcAgentTransport(factory, NullLogger<GrpcAgentTransport>.Instance);

        var result = await transport.SendAsync(new AgentInvocationRequest(
            AgentName: "agent-1",
            CorrelationId: "corr-barrier",
            Options: new AgentInvocationOptions(
                Timeout: TimeSpan.FromSeconds(2),
                MaxRetries: 0,
                TargetEndpoint: fixture.Endpoint)));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("BARRIER_VALIDATION_FAILED");
        result.ErrorMessage.Should().Contain("No explicit barrier");
    }

    [Fact]
    public async Task Invoke_returns_barrier_validation_failure_for_unknown_level()
    {
        await using var fixture = await GrpcTransportTestFixture.StartAsync(new EchoTransport());

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);
        using var transport = new GrpcAgentTransport(factory, NullLogger<GrpcAgentTransport>.Instance);

        var channel = factory.GetOrCreate(fixture.Endpoint);
        var client = new AgentTransportService.AgentTransportServiceClient(channel);
        var headers = new Metadata { { "x-nexo-barrier", "top-secret" } };

        var response = await client.InvokeAsync(
            new InvokeRequest
            {
                AgentName = "agent-1",
                CorrelationId = "corr-unknown-barrier",
                TimeoutMs = 2000,
            },
            headers: headers,
            deadline: DateTime.UtcNow.AddSeconds(2));

        response.Success.Should().BeFalse();
        response.ErrorCode.Should().Be("BARRIER_VALIDATION_FAILED");
        response.ErrorMessage.Should().Contain("Unknown barrier level");
    }

    [Fact]
    public async Task Invoke_surfaces_transport_failure_from_inner_transport()
    {
        await using var fixture = await GrpcTransportTestFixture.StartAsync(new FailingTransport("agent exploded"));

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);
        using var transport = new GrpcAgentTransport(factory, NullLogger<GrpcAgentTransport>.Instance);

        var headers = new Metadata { { "x-nexo-barrier", "public" } };
        var channel = factory.GetOrCreate(fixture.Endpoint);
        var client = new AgentTransportService.AgentTransportServiceClient(channel);
        var response = await client.InvokeAsync(
            new InvokeRequest
            {
                AgentName = "agent-1",
                CorrelationId = "corr-fail",
                TimeoutMs = 2000,
            },
            headers: headers,
            deadline: DateTime.UtcNow.AddSeconds(2));

        response.Success.Should().BeFalse();
        response.ErrorMessage.Should().Contain("agent exploded");
    }

    [Fact]
    public async Task CheckHealth_round_trips_server_health()
    {
        await using var fixture = await GrpcTransportTestFixture.StartAsync(new EchoTransport());

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);
        using var transport = new GrpcAgentTransport(factory, NullLogger<GrpcAgentTransport>.Instance);

        var health = await transport.CheckEndpointHealthAsync(fixture.Endpoint);

        health.IsHealthy.Should().BeTrue();
        health.TransportType.Should().Be("echo");
    }

    [Fact]
    public async Task Invoke_serializes_scalar_output_under_result_key()
    {
        await using var fixture = await GrpcTransportTestFixture.StartAsync(new ScalarOutputTransport());

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);

        var channel = factory.GetOrCreate(fixture.Endpoint);
        var client = new AgentTransportService.AgentTransportServiceClient(channel);
        var response = await client.InvokeAsync(
            new InvokeRequest
            {
                AgentName = "agent-1",
                CorrelationId = "corr-scalar",
                TimeoutMs = 2000,
            },
            headers: new Metadata { { "x-nexo-barrier", "public" } },
            deadline: DateTime.UtcNow.AddSeconds(2));

        response.Success.Should().BeTrue();
        response.Output.Should().ContainKey("result");
    }

    [Fact]
    public async Task Invoke_applies_default_floor_when_no_barrier_header()
    {
        await using var fixture = await GrpcTransportTestFixture.StartAsync(new EchoTransport());

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);

        var channel = factory.GetOrCreate(fixture.Endpoint);
        var client = new AgentTransportService.AgentTransportServiceClient(channel);
        var response = await client.InvokeAsync(
            new InvokeRequest
            {
                AgentName = "agent-1",
                CorrelationId = "corr-default-floor",
                TimeoutMs = 2000,
            },
            deadline: DateTime.UtcNow.AddSeconds(2));

        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_round_trips_payload_metadata_and_correlation_header()
    {
        await using var fixture = await GrpcTransportTestFixture.StartAsync(new EchoTransport());

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);

        var channel = factory.GetOrCreate(fixture.Endpoint);
        var client = new AgentTransportService.AgentTransportServiceClient(channel);
        var headers = new Metadata
        {
            { "x-nexo-barrier", "public" },
            { "x-nexo-correlation-id", "corr-from-header" },
        };

        var request = new InvokeRequest
        {
            AgentName = "agent-1",
            CorrelationId = "corr-body",
            TimeoutMs = 2000,
            MaxRetries = 1,
            TargetEndpoint = "https://remote.example",
        };
        request.Payload["answer"] = "42";
        request.Metadata["trace"] = "abc";

        var response = await client.InvokeAsync(
            request,
            headers: headers,
            deadline: DateTime.UtcNow.AddSeconds(2));

        response.Success.Should().BeTrue();
        response.CorrelationId.Should().Be("corr-from-header");
        response.Output.Should().ContainKey("answer");
    }

    [Fact]
    public async Task Invoke_serializes_dictionary_output_without_result_wrapper()
    {
        await using var fixture = await GrpcTransportTestFixture.StartAsync(new DictionaryOutputTransport());

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);

        var channel = factory.GetOrCreate(fixture.Endpoint);
        var client = new AgentTransportService.AgentTransportServiceClient(channel);
        var response = await client.InvokeAsync(
            new InvokeRequest
            {
                AgentName = "agent-1",
                CorrelationId = "corr-dict",
                TimeoutMs = 2000,
            },
            headers: new Metadata { { "x-nexo-barrier", "public" } },
            deadline: DateTime.UtcNow.AddSeconds(2));

        response.Success.Should().BeTrue();
        response.Output.Should().ContainKey("alpha");
        response.Output.Should().NotContainKey("result");
    }

    [Fact]
    public async Task Invoke_uses_metadata_error_code_when_transport_error_code_missing()
    {
        await using var fixture = await GrpcTransportTestFixture.StartAsync(new MetadataErrorTransport());

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);

        var channel = factory.GetOrCreate(fixture.Endpoint);
        var client = new AgentTransportService.AgentTransportServiceClient(channel);
        var response = await client.InvokeAsync(
            new InvokeRequest
            {
                AgentName = "agent-1",
                CorrelationId = "corr-meta-error",
                TimeoutMs = 2000,
            },
            headers: new Metadata { { "x-nexo-barrier", "public" } },
            deadline: DateTime.UtcNow.AddSeconds(2));

        response.Success.Should().BeFalse();
        response.ErrorCode.Should().Be("META_CODE");
    }

    [Fact]
    public async Task CheckHealth_uses_transport_name_when_type_missing()
    {
        await using var fixture = await GrpcTransportTestFixture.StartAsync(new NameOnlyHealthTransport());

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);

        var channel = factory.GetOrCreate(fixture.Endpoint);
        var client = new AgentTransportService.AgentTransportServiceClient(channel);
        var response = await client.CheckHealthAsync(new HealthRequest(), deadline: DateTime.UtcNow.AddSeconds(2));

        response.IsHealthy.Should().BeTrue();
        response.TransportType.Should().Be("name-only");
        response.DiagnosticMessage.Should().Be("ready");
    }

    [Fact]
    public async Task Invoke_accepts_non_bearer_authorization_and_api_key_headers()
    {
        await using var fixture = await GrpcTransportTestFixture.StartAsync(new EchoTransport());

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);

        var channel = factory.GetOrCreate(fixture.Endpoint);
        var client = new AgentTransportService.AgentTransportServiceClient(channel);
        var headers = new Metadata
        {
            { "x-nexo-barrier", "public" },
            { "authorization", "ApiKey abc123" },
            { "x-nexo-api-key", "secret-key" },
        };

        var response = await client.InvokeAsync(
            new InvokeRequest
            {
                AgentName = "agent-1",
                CorrelationId = "corr-auth",
                TimeoutMs = 2000,
            },
            headers: headers,
            deadline: DateTime.UtcNow.AddSeconds(2));

        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_treats_invalid_payload_json_as_opaque_string()
    {
        await using var fixture = await GrpcTransportTestFixture.StartAsync(new EchoTransport());

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);

        var channel = factory.GetOrCreate(fixture.Endpoint);
        var client = new AgentTransportService.AgentTransportServiceClient(channel);
        var request = new InvokeRequest
        {
            AgentName = "agent-1",
            CorrelationId = "corr-bad-json",
            TimeoutMs = 2000,
        };
        request.Payload["note"] = "not-json{{";

        var response = await client.InvokeAsync(
            request,
            headers: new Metadata { { "x-nexo-barrier", "public" } },
            deadline: DateTime.UtcNow.AddSeconds(2));

        response.Success.Should().BeTrue();
        response.Output.Should().ContainKey("note");
    }

    [Fact]
    public async Task Invoke_returns_explicit_transport_error_code_when_present()
    {
        await using var fixture = await GrpcTransportTestFixture.StartAsync(new ExplicitErrorCodeTransport());

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);

        var channel = factory.GetOrCreate(fixture.Endpoint);
        var client = new AgentTransportService.AgentTransportServiceClient(channel);
        var response = await client.InvokeAsync(
            new InvokeRequest
            {
                AgentName = "agent-1",
                CorrelationId = "corr-explicit-code",
                TimeoutMs = 2000,
            },
            headers: new Metadata { { "x-nexo-barrier", "public" } },
            deadline: DateTime.UtcNow.AddSeconds(2));

        response.Success.Should().BeFalse();
        response.ErrorCode.Should().Be("EXPLICIT");
    }

    [Fact]
    public async Task Invoke_serializes_non_nullable_object_dictionary_output()
    {
        await using var fixture = await GrpcTransportTestFixture.StartAsync(new ObjectDictionaryOutputTransport());

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);

        var channel = factory.GetOrCreate(fixture.Endpoint);
        var client = new AgentTransportService.AgentTransportServiceClient(channel);
        var response = await client.InvokeAsync(
            new InvokeRequest
            {
                AgentName = "agent-1",
                CorrelationId = "corr-object-dict",
                TimeoutMs = 2000,
            },
            headers: new Metadata { { "x-nexo-barrier", "public" } },
            deadline: DateTime.UtcNow.AddSeconds(2));

        response.Success.Should().BeTrue();
        response.Output.Should().ContainKey("count");
        response.Output["count"].Should().Be("7");
    }

    private sealed class ObjectDictionaryOutputTransport : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentResult(
                Success: true,
                Output: new Dictionary<string, object> { ["count"] = 7 },
                CorrelationId: request.CorrelationId,
                SpanId: request.SpanId));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransportHealth(true, "object-dict"));
    }

    private sealed class ExplicitErrorCodeTransport : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentResult(
                Success: false,
                ErrorMessage: "failed",
                ErrorCode: "EXPLICIT",
                CorrelationId: request.CorrelationId,
                SpanId: request.SpanId));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransportHealth(false, "explicit-error"));
    }

    private sealed class DictionaryOutputTransport : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentResult(
                Success: true,
                Output: new Dictionary<string, object?> { ["alpha"] = 1 },
                CorrelationId: request.CorrelationId,
                SpanId: request.SpanId));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransportHealth(true, "dict"));
    }

    private sealed class MetadataErrorTransport : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentResult(
                Success: false,
                ErrorMessage: "failed",
                Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["errorCode"] = "META_CODE",
                },
                CorrelationId: request.CorrelationId,
                SpanId: request.SpanId));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransportHealth(false, "meta-error"));
    }

    private sealed class NameOnlyHealthTransport : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentResult(
                Success: true,
                Output: new Dictionary<string, object?>(),
                CorrelationId: request.CorrelationId,
                SpanId: request.SpanId));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransportHealth(
                IsHealthy: true,
                TransportName: "name-only",
                Message: "ready"));
    }

    private sealed class ScalarOutputTransport : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentResult(
                Success: true,
                Output: 42,
                CorrelationId: request.CorrelationId,
                SpanId: request.SpanId));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransportHealth(true, "scalar"));
    }

    private sealed class EchoTransport : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentResult(
                Success: true,
                Output: request.Payload ?? new Dictionary<string, object?>(),
                CorrelationId: request.CorrelationId,
                SpanId: request.SpanId));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransportHealth(
                IsHealthy: true,
                TransportName: "echo",
                Message: "ok",
                TransportType: "echo",
                DiagnosticMessage: "ok"));
    }

    private sealed class FailingTransport : IAgentTransport
    {
        private readonly string _message;
        public FailingTransport(string message) => _message = message;

        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentResult(
                Success: false,
                ErrorMessage: _message,
                ErrorCode: "AGENT_FAILED",
                CorrelationId: request.CorrelationId,
                SpanId: request.SpanId));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransportHealth(false, "fail"));
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _key;
        private readonly string? _priorValue;

        public EnvironmentVariableScope(string key, string? value)
        {
            _key = key;
            _priorValue = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }

        public void Dispose() => Environment.SetEnvironmentVariable(_key, _priorValue);
    }
}

internal sealed class GrpcTransportTestFixture : IAsyncDisposable
{
    private readonly WebApplication _app;

    private GrpcTransportTestFixture(WebApplication app, string endpoint)
    {
        _app = app;
        Endpoint = endpoint;
    }

    public string Endpoint { get; }

    public static async Task<GrpcTransportTestFixture> StartAsync(
        IAgentTransport localTransport,
        bool requireExplicitBarrier = false)
    {
        var port = GetFreePort();
        var endpoint = $"http://127.0.0.1:{port}";

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, port, listen =>
            {
                listen.Protocols = HttpProtocols.Http2;
            });
        });

        builder.Services.AddSingleton<IAgentTransport>(localTransport);
        builder.Services.AddSingleton<BarrierHierarchy>(_ =>
            new BarrierHierarchy([new BarrierLevel("public", 0), new BarrierLevel("internal", 1)]));
        builder.Services.AddSingleton<IOptions<BarrierOptions>>(
            _ => Options.Create(new BarrierOptions
            {
                Levels = ["public", "internal"],
                RequireExplicitBarrier = requireExplicitBarrier
            }));
        builder.Services.AddScoped<IBarrierContextAccessor, ScopedBarrierContextAccessor>();
        builder.Services.AddSingleton<IBarrierAuditLog, StructuredBarrierAuditLog>();
        builder.Services.AddNexoGrpcServer();

        var app = builder.Build();
        app.MapNexoGrpcServer();
        await app.StartAsync();

        return new GrpcTransportTestFixture(app, endpoint);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
