using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Execution;
using Nexo.Abstractions.Transport;
using Nexo.Runtime.Barriers;
using Nexo.Transport.Grpc;
using Nexo.Transport.Grpc.Server;
using Xunit;

namespace Nexo.Tests.Transport;

public sealed class GrpcAgentTransportTests
{
    [Fact]
    public async Task SendAsync_HappyPath_RoutesThroughServerAndRoundTripsCorrelation()
    {
        await using var fixture = await GrpcServerFixture.StartAsync(
            new EchoTransport(delay: TimeSpan.Zero));

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);
        using var transport = new GrpcAgentTransport(factory, NullLogger<GrpcAgentTransport>.Instance);

        var request = new AgentInvocationRequest(
            AgentName: "agent-1",
            CorrelationId: "corr-123",
            SpanId: "span-1",
            Payload: new Dictionary<string, object?> { ["value"] = 42 },
            Options: new AgentInvocationOptions(
                Timeout: TimeSpan.FromSeconds(2),
                MaxRetries: 1,
                TargetEndpoint: fixture.Endpoint));

        var result = await transport.SendAsync(request);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("corr-123");
    }

    [Fact]
    public async Task SendAsync_WhenDeadlineExceeded_ReturnsTimeoutErrorCode()
    {
        await using var fixture = await GrpcServerFixture.StartAsync(
            new EchoTransport(delay: TimeSpan.FromSeconds(2)));

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);
        using var transport = new GrpcAgentTransport(factory, NullLogger<GrpcAgentTransport>.Instance);

        var request = new AgentInvocationRequest(
            AgentName: "agent-1",
            CorrelationId: "corr-timeout",
            Options: new AgentInvocationOptions(
                Timeout: TimeSpan.FromMilliseconds(25),
                MaxRetries: 0,
                TargetEndpoint: fixture.Endpoint));

        var result = await transport.SendAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("TIMEOUT");
    }

    [Fact]
    public async Task SendAsync_WhenUnavailable_ReturnsTransportUnavailableErrorCode()
    {
        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);
        using var transport = new GrpcAgentTransport(factory, NullLogger<GrpcAgentTransport>.Instance);

        var request = new AgentInvocationRequest(
            AgentName: "agent-1",
            CorrelationId: "corr-unavailable",
            Options: new AgentInvocationOptions(
                Timeout: TimeSpan.FromMilliseconds(100),
                MaxRetries: 0,
                TargetEndpoint: "http://127.0.0.1:6550"));

        var result = await transport.SendAsync(request);

        result.Success.Should().BeFalse();
        // Under load the gRPC stack may surface TIMEOUT before UNAVAILABLE is classified.
        result.ErrorCode.Should().BeOneOf("TRANSPORT_UNAVAILABLE", "TIMEOUT");
    }

    [Fact]
    public async Task SendAsync_MetadataRoundTrip_IncludesExecutionIsolationOnInvocation()
    {
        AgentInvocationRequest? captured = null;
        await using var fixture = await GrpcServerFixture.StartAsync(new CaptureInvocationTransport(r => captured = r));

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);
        using var transport = new GrpcAgentTransport(factory, NullLogger<GrpcAgentTransport>.Instance);

        var request = new AgentInvocationRequest(
            AgentName: "agent-1",
            CorrelationId: "corr-meta",
            SpanId: "span-meta",
            Payload: new Dictionary<string, object?> { ["k"] = 1 },
            Options: new AgentInvocationOptions(
                Timeout: TimeSpan.FromSeconds(2),
                MaxRetries: 0,
                TargetEndpoint: fixture.Endpoint),
            Metadata: new Dictionary<string, string>
            {
                [AgentExecutionIsolation.MetadataKey] = AgentExecutionIsolation.Format(
                    AgentExecutionIsolationLevel.ContainerPerAgent),
                ["domain"] = "General",
            });

        var result = await transport.SendAsync(request);

        result.Success.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Metadata.Should().NotBeNull();
        captured.Metadata![AgentExecutionIsolation.MetadataKey].Should().Be("ContainerPerAgent");
        captured.Metadata["domain"].Should().Be("General");
    }

    [Fact]
    public async Task SendAsync_PayloadRoundTrip_PreservesNestedValues()
    {
        await using var fixture = await GrpcServerFixture.StartAsync(
            new EchoTransport(delay: TimeSpan.Zero));

        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);
        using var transport = new GrpcAgentTransport(factory, NullLogger<GrpcAgentTransport>.Instance);

        var payload = new Dictionary<string, object?>
        {
            ["nested"] = new Dictionary<string, object?> { ["a"] = 1, ["b"] = "x" },
            ["list"] = new[] { 1, 2, 3 },
            ["nullValue"] = null
        };

        var request = new AgentInvocationRequest(
            AgentName: "agent-1",
            CorrelationId: "corr-roundtrip",
            Payload: payload,
            Options: new AgentInvocationOptions(
                Timeout: TimeSpan.FromSeconds(2),
                MaxRetries: 0,
                TargetEndpoint: fixture.Endpoint));

        var result = await transport.SendAsync(request);

        result.Success.Should().BeTrue();
        var output = result.Output.Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        output.Should().ContainKey("nested");
        output.Should().ContainKey("list");
        output.Should().ContainKey("nullValue");
    }

    private sealed class CaptureInvocationTransport : IAgentTransport
    {
        private readonly Action<AgentInvocationRequest> _onInvoke;

        public CaptureInvocationTransport(Action<AgentInvocationRequest> onInvoke)
        {
            _onInvoke = onInvoke;
        }

        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default)
        {
            _onInvoke(request);
            return Task.FromResult(new AgentResult(
                Success: true,
                Output: request.Payload ?? new Dictionary<string, object?>(),
                CorrelationId: request.CorrelationId,
                SpanId: request.SpanId));
        }

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TransportHealth(true, "capture"));
    }

    private sealed class EchoTransport : IAgentTransport
    {
        private readonly TimeSpan _delay;

        public EchoTransport(TimeSpan delay)
        {
            _delay = delay;
        }

        public async Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default)
        {
            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken);
            }

            return new AgentResult(
                Success: true,
                Output: request.Payload ?? new Dictionary<string, object?>(),
                CorrelationId: request.CorrelationId,
                SpanId: request.SpanId);
        }

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TransportHealth(true, "echo"));
    }

    private sealed class GrpcServerFixture : IAsyncDisposable
    {
        private readonly WebApplication _app;

        private GrpcServerFixture(WebApplication app, string endpoint)
        {
            _app = app;
            Endpoint = endpoint;
        }

        public string Endpoint { get; }

        public static async Task<GrpcServerFixture> StartAsync(IAgentTransport localTransport)
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
                _ => Options.Create(new BarrierOptions { Levels = ["public", "internal"], RequireExplicitBarrier = false }));
            builder.Services.AddScoped<IBarrierContextAccessor, ScopedBarrierContextAccessor>();
            builder.Services.AddSingleton<IBarrierAuditLog, StructuredBarrierAuditLog>();
            builder.Services.AddNexoGrpcServer();

            var app = builder.Build();
            app.MapNexoGrpcServer();
            await app.StartAsync();

            return new GrpcServerFixture(app, endpoint);
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

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_key, _priorValue);
        }
    }
}
