using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Abstractions.Transport;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Transport;
using Xunit;

namespace Nexo.Tests.Orchestration;

/// <summary>Tests for orchestration transport gap coverage.</summary>
public class OrchestrationTransportGapCoverageTests
{
    [Fact]
    public async Task SendAsync_null_request_throws()
    {
        var transport = CreateTransport();
        var act = () => transport.SendAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_uses_dependency_outputs_when_provided()
    {
        var lifecycle = CreateLifecycleManager();
        var agent = new CapturingDependencyAgent(
            new AgentSpawnSpec { AgentId = "dep-cap", Domain = "Test", Goal = "capture" },
            NullLogger<BaseAgent>.Instance);
        var container = new AgentContainer(agent, NullLogger<AgentContainer>.Instance);
        await lifecycle.RegisterAgentAsync(container);

        var transport = CreateTransport(lifecycle);
        var result = await transport.SendAsync(new AgentInvocationRequest(
            "dep-cap",
            "corr-dep",
            DependencyOutputs: new Dictionary<string, object> { ["upstream"] = "value" }));

        result.Success.Should().BeTrue();
        agent.LastDependencies.Should().ContainKey("upstream");
    }

    [Fact]
    public async Task SendAsync_maps_payload_when_dependency_outputs_absent()
    {
        var lifecycle = CreateLifecycleManager();
        var agent = new CapturingDependencyAgent(
            new AgentSpawnSpec { AgentId = "payload-cap", Domain = "Test", Goal = "capture" },
            NullLogger<BaseAgent>.Instance);
        var container = new AgentContainer(agent, NullLogger<AgentContainer>.Instance);
        await lifecycle.RegisterAgentAsync(container);

        var transport = CreateTransport(lifecycle);
        var result = await transport.SendAsync(new AgentInvocationRequest(
            "payload-cap",
            "corr-payload",
            SpanId: "span-1",
            Payload: new Dictionary<string, object?> { ["key"] = "payload-value" }));

        result.Success.Should().BeTrue();
        result.SpanId.Should().Be("span-1");
        agent.LastDependencies.Should().ContainKey("key");
    }

    [Fact]
    public async Task SendAsync_returns_timeout_error_code_when_agent_times_out()
    {
        var lifecycle = CreateLifecycleManager();
        var agent = new TimeoutExecuteAgent(
            new AgentSpawnSpec { AgentId = "timeout-agent", Domain = "Test", Goal = "slow" },
            NullLogger<BaseAgent>.Instance);
        var container = new AgentContainer(agent, NullLogger<AgentContainer>.Instance);
        await lifecycle.RegisterAgentAsync(container);

        var transport = CreateTransport(lifecycle);
        var result = await transport.SendAsync(new AgentInvocationRequest("timeout-agent", "corr-timeout"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("TIMEOUT");
        result.Metadata!["errorCode"].Should().Be("TIMEOUT");
    }

    [Fact]
    public async Task SendAsync_returns_transport_error_for_unhandled_failures()
    {
        var lifecycle = CreateLifecycleManager();
        var agent = new BoomExecuteAgent(
            new AgentSpawnSpec { AgentId = "boom-agent", Domain = "Test", Goal = "fail" },
            NullLogger<BaseAgent>.Instance);
        var container = new AgentContainer(agent, NullLogger<AgentContainer>.Instance);
        await lifecycle.RegisterAgentAsync(container);

        var transport = CreateTransport(lifecycle);
        var result = await transport.SendAsync(new AgentInvocationRequest("boom-agent", "corr-boom"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("TRANSPORT_ERROR");
        result.ErrorMessage.Should().Contain("boom");
    }

    [Fact]
    public async Task SendAsync_not_registered_still_maps_agent_not_found()
    {
        var transport = CreateTransport();
        var result = await transport.SendAsync(new AgentInvocationRequest("ghost", "corr-ghost"));
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("AGENT_NOT_FOUND");
    }

    /// <summary>Creates transport.</summary>
    /// <param name="null">Null.</param>
    private static InProcessAgentTransport CreateTransport(LifecycleManager? lifecycle = null) =>
        new(
            lifecycle ?? CreateLifecycleManager(),
            NullLogger<InProcessAgentTransport>.Instance);

    /// <summary>Creates lifecycle manager.</summary>
    private static LifecycleManager CreateLifecycleManager() =>
        new(
            NullLogger<LifecycleManager>.Instance,
            new HealthMonitor(NullLogger<HealthMonitor>.Instance, TimeSpan.FromHours(1)));

    /// <summary>Capturing dependency agent.</summary>
    private sealed class CapturingDependencyAgent : BaseAgent
    {
        /// <summary>Last dependencies.</summary>
        public IReadOnlyDictionary<string, object>? LastDependencies { get; private set; }

        public CapturingDependencyAgent(AgentSpawnSpec spec, ILogger<BaseAgent> logger) : base(spec, logger) { }

        /// <summary>On initialize async.</summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        /// <summary>On dependencies resolved async.</summary>
        /// <param name="dependencyOutputs">Dependency outputs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task OnDependenciesResolvedAsync(IReadOnlyDictionary<string, object> dependencyOutputs, CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task<object> OnExecuteAsync(IReadOnlyDictionary<string, object>? dependencyOutputs, CancellationToken cancellationToken)
        {
            LastDependencies = dependencyOutputs;
            return Task.FromResult<object>("ok");
        }
        /// <summary>On shutdown async.</summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task OnShutdownAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>Timeout execute agent.</summary>
    private sealed class TimeoutExecuteAgent : BaseAgent
    {
        public TimeoutExecuteAgent(AgentSpawnSpec spec, ILogger<BaseAgent> logger) : base(spec, logger) { }

        /// <summary>On initialize async.</summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        /// <summary>On dependencies resolved async.</summary>
        /// <param name="dependencyOutputs">Dependency outputs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task OnDependenciesResolvedAsync(IReadOnlyDictionary<string, object> dependencyOutputs, CancellationToken cancellationToken) => Task.CompletedTask;
        /// <summary>On execute async.</summary>
        /// <param name="dependencyOutputs">Dependency outputs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task<object> OnExecuteAsync(IReadOnlyDictionary<string, object>? dependencyOutputs, CancellationToken cancellationToken) =>
            /// <summary>Timeout exception.</summary>
            /// <param name="out"">Out".</param>
            throw new TimeoutException("agent timed out");
        /// <summary>On shutdown async.</summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task OnShutdownAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>Boom execute agent.</summary>
    private sealed class BoomExecuteAgent : BaseAgent
    {
        public BoomExecuteAgent(AgentSpawnSpec spec, ILogger<BaseAgent> logger) : base(spec, logger) { }

        /// <summary>On initialize async.</summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        /// <summary>On dependencies resolved async.</summary>
        /// <param name="dependencyOutputs">Dependency outputs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task OnDependenciesResolvedAsync(IReadOnlyDictionary<string, object> dependencyOutputs, CancellationToken cancellationToken) => Task.CompletedTask;
        /// <summary>On execute async.</summary>
        /// <param name="dependencyOutputs">Dependency outputs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task<object> OnExecuteAsync(IReadOnlyDictionary<string, object>? dependencyOutputs, CancellationToken cancellationToken) =>
            /// <summary>Invalid operation exception.</summary>
            throw new InvalidOperationException("boom");
        /// <summary>On shutdown async.</summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task OnShutdownAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
