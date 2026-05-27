using System.Collections.Concurrent;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Routing;
using Nexo.Abstractions.Transport;
using Nexo.Runtime.Barriers;
using Nexo.Runtime.Routing;
using Nexo.Transport.Grpc;
using Nexo.Transport.Grpc.Server;
using Xunit;

namespace Nexo.Tests.Orchestration.Routing;

public sealed class EndpointHealthMonitorTests
{
    [Fact]
    public async Task ExecuteAsync_HealthyProbe_UpdatesRegistryTrue()
    {
        await using var fixture = await GrpcServerFixture.StartAsync(health: true);
        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");

        var registry = new TestEndpointRegistry([
            new EndpointDescriptor(
                Endpoint: fixture.Endpoint,
                Name: "healthy",
                SupportedCapabilities: ["CodeGeneration"],
                AcceptedBarrierLevels: [],
                Region: null,
                Priority: 1,
                IsHealthy: false)
        ]);

        using var transport = CreateGrpcTransport();
        var monitor = new EndpointHealthMonitor(
            registry,
            transport,
            Options.Create(new RoutingOptions { HealthCheckIntervalSeconds = 30 }),
            NullLogger<EndpointHealthMonitor>.Instance);

        await monitor.StartAsync(CancellationToken.None);
        await Task.Delay(300);
        await monitor.StopAsync(CancellationToken.None);

        registry.Updates.Should().ContainSingle();
        registry.Updates[0].isHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_UnhealthyProbe_UpdatesRegistryFalse_AndLogsWarning()
    {
        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var endpoint = "http://127.0.0.1:6551";
        var registry = new TestEndpointRegistry([
            new EndpointDescriptor(
                Endpoint: endpoint,
                Name: "down",
                SupportedCapabilities: ["CodeGeneration"],
                AcceptedBarrierLevels: [],
                Region: null,
                Priority: 1,
                IsHealthy: true)
        ]);

        using var transport = CreateGrpcTransport();
        var logger = new Mock<ILogger<EndpointHealthMonitor>>();
        var monitor = new EndpointHealthMonitor(
            registry,
            transport,
            Options.Create(new RoutingOptions { HealthCheckIntervalSeconds = 30 }),
            logger.Object);

        await monitor.StartAsync(CancellationToken.None);
        await Task.Delay(300);
        await monitor.StopAsync(CancellationToken.None);

        registry.Updates.Should().ContainSingle();
        registry.Updates[0].isHealthy.Should().BeFalse();
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_UnhealthyProbe_DoesNotLogBeforeThreshold()
    {
        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var endpoint = "http://127.0.0.1:6552";
        var registry = new TestEndpointRegistry([
            new EndpointDescriptor(
                Endpoint: endpoint,
                Name: "down-threshold",
                SupportedCapabilities: ["CodeGeneration"],
                AcceptedBarrierLevels: [],
                Region: null,
                Priority: 1,
                IsHealthy: true)
        ]);

        using var transport = CreateGrpcTransport();
        var logger = new Mock<ILogger<EndpointHealthMonitor>>();
        var monitor = new EndpointHealthMonitor(
            registry,
            transport,
            Options.Create(new RoutingOptions
            {
                HealthCheckIntervalSeconds = 30,
                DegradedLogFailureThreshold = 2
            }),
            logger.Object);

        await monitor.StartAsync(CancellationToken.None);
        await Task.Delay(300);
        await monitor.StopAsync(CancellationToken.None);

        registry.Updates.Should().ContainSingle();
        registry.Updates[0].isHealthy.Should().BeFalse();
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_StopsPromptly()
    {
        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var registry = new TestEndpointRegistry([]);
        using var transport = CreateGrpcTransport();
        var monitor = new EndpointHealthMonitor(
            registry,
            transport,
            Options.Create(new RoutingOptions { HealthCheckIntervalSeconds = 30 }),
            NullLogger<EndpointHealthMonitor>.Instance);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await monitor.StartAsync(CancellationToken.None);
        await monitor.StopAsync(CancellationToken.None);
        sw.Stop();

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ExecuteAsync_SkipsEndpointsWithBlankUri()
    {
        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var registry = new TestEndpointRegistry([
            new EndpointDescriptor(
                Endpoint: "   ",
                Name: "blank",
                SupportedCapabilities: ["CodeGeneration"],
                AcceptedBarrierLevels: [],
                Region: null,
                Priority: 1,
                IsHealthy: true),
        ]);

        using var transport = CreateGrpcTransport();
        var monitor = new EndpointHealthMonitor(
            registry,
            transport,
            Options.Create(new RoutingOptions { HealthCheckIntervalSeconds = 30 }),
            NullLogger<EndpointHealthMonitor>.Instance);

        await monitor.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await monitor.StopAsync(CancellationToken.None);

        registry.Updates.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenUpdateHealthThrows_LogsWarningAfterThreshold()
    {
        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        await using var fixture = await GrpcServerFixture.StartAsync(health: true);
        var registry = new ThrowingUpdateEndpointRegistry([
            new EndpointDescriptor(
                Endpoint: fixture.Endpoint,
                Name: "throws",
                SupportedCapabilities: ["CodeGeneration"],
                AcceptedBarrierLevels: [],
                Region: null,
                Priority: 1,
                IsHealthy: true),
        ]);

        using var transport = CreateGrpcTransport();
        var logger = new Mock<ILogger<EndpointHealthMonitor>>();
        var monitor = new EndpointHealthMonitor(
            registry,
            transport,
            Options.Create(new RoutingOptions
            {
                HealthCheckIntervalSeconds = 1,
                DegradedLogFailureThreshold = 1,
            }),
            logger.Object);

        await monitor.StartAsync(CancellationToken.None);
        await Task.Delay(2500);
        await monitor.StopAsync(CancellationToken.None);

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private sealed class ThrowingUpdateEndpointRegistry : IEndpointRegistry
    {
        private readonly ConcurrentDictionary<string, EndpointDescriptor> _endpoints =
            new(StringComparer.OrdinalIgnoreCase);

        public ThrowingUpdateEndpointRegistry(IReadOnlyList<EndpointDescriptor> endpoints)
        {
            foreach (var endpoint in endpoints)
                _endpoints[endpoint.Endpoint] = endpoint;
        }

        public List<(string endpoint, bool isHealthy)> Updates { get; } = [];

        public IReadOnlyList<EndpointDescriptor> GetAll() => _endpoints.Values.ToList();

        public void Register(EndpointDescriptor descriptor) => _endpoints[descriptor.Endpoint] = descriptor;

        public void UpdateHealth(string endpoint, bool isHealthy)
        {
            Updates.Add((endpoint, isHealthy));
            if (isHealthy)
                throw new InvalidOperationException("update failed");
        }
    }

    private static GrpcAgentTransport CreateGrpcTransport()
    {
        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);
        return new GrpcAgentTransport(factory, NullLogger<GrpcAgentTransport>.Instance);
    }

    private sealed class TestEndpointRegistry : IEndpointRegistry
    {
        private readonly ConcurrentDictionary<string, EndpointDescriptor> _endpoints =
            new(StringComparer.OrdinalIgnoreCase);

        public TestEndpointRegistry(IReadOnlyList<EndpointDescriptor> endpoints)
        {
            foreach (var endpoint in endpoints)
            {
                _endpoints[endpoint.Endpoint] = endpoint;
            }
        }

        public List<(string endpoint, bool isHealthy)> Updates { get; } = [];

        public IReadOnlyList<EndpointDescriptor> GetAll() => _endpoints.Values.ToList();

        public void Register(EndpointDescriptor descriptor)
        {
            _endpoints[descriptor.Endpoint] = descriptor;
        }

        public void UpdateHealth(string endpoint, bool isHealthy)
        {
            Updates.Add((endpoint, isHealthy));
            if (_endpoints.TryGetValue(endpoint, out var descriptor))
            {
                _endpoints[endpoint] = descriptor with { IsHealthy = isHealthy };
            }
        }
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

        public static async Task<GrpcServerFixture> StartAsync(bool health)
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

            builder.Services.AddSingleton<IAgentTransport>(new FixedHealthTransport(health));
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

    private sealed class FixedHealthTransport : IAgentTransport
    {
        private readonly bool _healthy;

        public FixedHealthTransport(bool healthy)
        {
            _healthy = healthy;
        }

        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new AgentResult(Success: true));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TransportHealth(
                IsHealthy: _healthy,
                TransportName: "fixed",
                Message: _healthy ? "ok" : "down",
                TransportType: "gRPC",
                DiagnosticMessage: _healthy ? "ok" : "down"));
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _key;
        private readonly string? _prior;

        public EnvironmentVariableScope(string key, string? value)
        {
            _key = key;
            _prior = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_key, _prior);
        }
    }
}
