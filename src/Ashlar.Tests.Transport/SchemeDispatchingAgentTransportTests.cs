using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Abstractions.Transport;
using Ashlar.Runtime;
using Ashlar.Runtime.Transport;
using Xunit;

namespace Ashlar.Tests.Transport;

/// <summary>
/// The scheme-dispatching remote transport lets protocol adapters (A2A) coexist with the
/// default remote (gRPC) behind the single-remote RoutingAgentTransport, selected by
/// target-endpoint prefix.
/// </summary>
public sealed class SchemeDispatchingAgentTransportTests
{
    private sealed class RecordingTransport : IAgentTransport
    {
        private readonly bool _healthy;

        public RecordingTransport(string name, bool healthy = true) => (Name, _healthy) = (name, healthy);

        public string Name { get; }

        public AgentInvocationRequest? LastRequest { get; private set; }

        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new AgentResult(Success: true, Output: Name));
        }

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TransportHealth(_healthy, Name));
    }

    private static AgentInvocationRequest Request(string? endpoint) => new(
        AgentName: "agent",
        CorrelationId: "corr-1",
        Options: new AgentInvocationOptions(TimeSpan.FromSeconds(5), 0, endpoint));

    [Fact]
    public async Task Matching_prefix_dispatches_to_the_registered_transport_with_prefix_intact()
    {
        var a2a = new RecordingTransport("a2a");
        var fallback = new RecordingTransport("grpc");
        var dispatcher = new SchemeDispatchingAgentTransport(
            new[] { new KeyValuePair<string, IAgentTransport>("a2a+", a2a) },
            fallback,
            NullLogger<SchemeDispatchingAgentTransport>.Instance);

        var result = await dispatcher.SendAsync(Request("a2a+https://peer.example.com"));

        result.Output.Should().Be("a2a");
        a2a.LastRequest!.EffectiveOptions.TargetEndpoint.Should().Be(
            "a2a+https://peer.example.com", "each transport owns stripping its own scheme");
        fallback.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task Unmatched_endpoint_falls_back_to_the_default_remote()
    {
        var a2a = new RecordingTransport("a2a");
        var fallback = new RecordingTransport("grpc");
        var dispatcher = new SchemeDispatchingAgentTransport(
            new[] { new KeyValuePair<string, IAgentTransport>("a2a+", a2a) },
            fallback,
            NullLogger<SchemeDispatchingAgentTransport>.Instance);

        var result = await dispatcher.SendAsync(Request("https://grpc.example.com:5001"));

        result.Output.Should().Be("grpc");
        a2a.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task Prefix_matching_is_case_insensitive()
    {
        var a2a = new RecordingTransport("a2a");
        var dispatcher = new SchemeDispatchingAgentTransport(
            new[] { new KeyValuePair<string, IAgentTransport>("a2a+", a2a) },
            new RecordingTransport("grpc"),
            NullLogger<SchemeDispatchingAgentTransport>.Instance);

        var result = await dispatcher.SendAsync(Request("A2A+https://peer.example.com"));

        result.Output.Should().Be("a2a");
    }

    [Fact]
    public async Task Health_aggregates_scheme_transports_and_fallback()
    {
        var dispatcher = new SchemeDispatchingAgentTransport(
            new[] { new KeyValuePair<string, IAgentTransport>("a2a+", new RecordingTransport("a2a", healthy: false)) },
            new RecordingTransport("grpc"),
            NullLogger<SchemeDispatchingAgentTransport>.Instance);

        var health = await dispatcher.CheckHealthAsync();

        health.IsHealthy.Should().BeFalse();
        health.DiagnosticMessage.Should().Contain("a2a+");
    }

    [Fact]
    public async Task AddAshlarRuntimeTransport_wraps_remote_when_scheme_registrations_exist()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var a2a = new RecordingTransport("a2a");
        services.AddSingleton(new AgentTransportSchemeRegistration("a2a+", _ => a2a));
        services.AddAshlarRuntimeTransport<RecordingInProcess, RecordingRemote>();
        using var provider = services.BuildServiceProvider();

        var transport = provider.GetRequiredService<IAgentTransport>();
        var viaScheme = await transport.SendAsync(Request("a2a+https://peer"));
        var viaRemote = await transport.SendAsync(Request("https://grpc-peer"));
        var inProcess = await transport.SendAsync(Request(null));

        viaScheme.Output.Should().Be("a2a");
        viaRemote.Output.Should().Be("remote");
        inProcess.Output.Should().Be("in-process");
    }

    [Fact]
    public async Task AddAshlarRuntimeTransport_without_registrations_keeps_previous_composition()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAshlarRuntimeTransport<RecordingInProcess, RecordingRemote>();
        using var provider = services.BuildServiceProvider();

        var transport = provider.GetRequiredService<IAgentTransport>();
        var viaRemote = await transport.SendAsync(Request("a2a+https://peer"));

        viaRemote.Output.Should().Be(
            "remote", "with no scheme registrations, every remote endpoint goes to the default remote transport");
    }

    private sealed class RecordingInProcess : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new AgentResult(Success: true, Output: "in-process"));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TransportHealth(true, "in-process"));
    }

    private sealed class RecordingRemote : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new AgentResult(Success: true, Output: "remote"));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TransportHealth(true, "remote"));
    }
}
