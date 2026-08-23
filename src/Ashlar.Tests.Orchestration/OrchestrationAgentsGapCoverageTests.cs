using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Agents.Assets;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Assets.Models;
using Ashlar.Orchestration.Assets.Ports;
using System.Reflection;
using Xunit;

namespace Ashlar.Tests.Orchestration;

/// <summary>Tests for orchestration agents gap coverage.</summary>
public class OrchestrationAgentsGapCoverageTests
{
    [Fact]
    public async Task LifecycleManager_registers_executes_and_shuts_down_agents()
    {
        var monitor = new HealthMonitor(NullLogger<HealthMonitor>.Instance, TimeSpan.FromHours(1));
        var manager = new LifecycleManager(NullLogger<LifecycleManager>.Instance, monitor);
        var container = CreateGenericContainer("life-1");

        var registered = await manager.RegisterAgentAsync(container);
        registered.AgentId.Should().Be("life-1");
        manager.GetActiveAgents().Should().ContainSingle();
        manager.GetAgent("life-1").Should().NotBeNull();

        var output = await manager.ExecuteAgentAsync("life-1");
        output.Should().NotBeNull();

        await manager.ShutdownAgentAsync("life-1");
        manager.GetAgent("life-1").Should().BeNull();
        monitor.Dispose();
    }

    [Fact]
    public async Task LifecycleManager_shutdown_all_and_hot_reload_replace_agent()
    {
        var monitor = new HealthMonitor(NullLogger<HealthMonitor>.Instance, TimeSpan.FromHours(1));
        var manager = new LifecycleManager(NullLogger<LifecycleManager>.Instance, monitor);

        await manager.RegisterAgentAsync(CreateGenericContainer("a1"));
        await manager.RegisterAgentAsync(CreateGenericContainer("a2"));
        manager.GetActiveAgents().Should().HaveCount(2);

        await manager.ShutdownAllAsync();
        manager.GetActiveAgents().Should().BeEmpty();

        var first = CreateGenericContainer("reload", goal: "first");
        await manager.RegisterAgentAsync(first);
        var reloaded = CreateGenericContainer("reload", goal: "second");
        (await manager.HotReloadAgentAsync(reloaded)).Agent.Spec.Goal.Should().Be("second");
        monitor.Dispose();
    }

    [Fact]
    public async Task LifecycleManager_rejects_invalid_handles_and_missing_agents()
    {
        var manager = new LifecycleManager(
            NullLogger<LifecycleManager>.Instance,
            new HealthMonitor(NullLogger<HealthMonitor>.Instance, TimeSpan.FromHours(1)));

        var actNull = () => manager.RegisterAgentAsync((IAgentHandle)null!);
        await actNull.Should().ThrowAsync<ArgumentNullException>();

        var remoteHandle = Mock.Of<IAgentHandle>();
        var actRemote = () => manager.RegisterAgentAsync(remoteHandle);
        await actRemote.Should().ThrowAsync<InvalidOperationException>();

        var actExecute = () => manager.ExecuteAgentAsync("missing");
        await actExecute.Should().ThrowAsync<InvalidOperationException>();

        await manager.ShutdownAgentAsync("missing");
    }

    [Fact]
    public async Task LifecycleManager_force_terminates_when_shutdown_fails()
    {
        var monitor = new HealthMonitor(NullLogger<HealthMonitor>.Instance, TimeSpan.FromHours(1));
        var manager = new LifecycleManager(NullLogger<LifecycleManager>.Instance, monitor);
        var container = new AgentContainer(
            new FailingShutdownAgent(
                new AgentSpawnSpec { AgentId = "bad-shutdown", Domain = "Test", Goal = "x" },
                NullLogger<BaseAgent>.Instance),
            NullLogger<AgentContainer>.Instance);

        await manager.RegisterAgentAsync(container);

        var act = () => manager.ShutdownAgentAsync("bad-shutdown");
        await act.Should().ThrowAsync<InvalidOperationException>();
        manager.GetAgent("bad-shutdown").Should().BeNull();
        monitor.Dispose();
    }

    [Fact]
    public async Task BaseAgent_covers_failure_and_dependency_wait_paths()
    {
        var spec = new AgentSpawnSpec
        {
            AgentId = "dep-agent",
            Domain = "Test",
            Goal = "wait",
            Dependencies = new[] { "upstream" },
        };
        var agent = new GenericAgent(spec, NullLogger<GenericAgent>.Instance);
        await agent.InitializeAsync();

        await agent.WaitForDependenciesAsync(new Dictionary<string, object>());
        agent.State.Should().Be(AgentState.WaitingForDependencies);

        await agent.WaitForDependenciesAsync(new Dictionary<string, object> { ["upstream"] = "ok" });
        agent.State.Should().Be(AgentState.Ready);

        var actInit = () => agent.InitializeAsync();
        await actInit.Should().ThrowAsync<InvalidOperationException>();

        /// <summary>Sets agent state.</summary>
        SetAgentState(agent, AgentState.Created);
        var actExecuteEarly = () => agent.ExecuteAsync();
        await actExecuteEarly.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task BaseAgent_execute_failure_and_think_async_paths()
    {
        var failing = new FailingExecuteAgent(
            new AgentSpawnSpec { AgentId = "fail-exec", Domain = "Test", Goal = "boom" },
            NullLogger<BaseAgent>.Instance);
        await failing.InitializeAsync();

        var act = () => failing.ExecuteAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
        failing.State.Should().Be(AgentState.Failed);
        failing.Health.Should().Be(AgentHealth.Unhealthy);

        var ready = new GenericAgent(
            new AgentSpawnSpec { AgentId = "think", Domain = "Test", Goal = "think" },
            NullLogger<GenericAgent>.Instance);
        await ready.InitializeAsync();
        (await ready.ThinkAsync(new AgentObservation(WorldSnapshot.ForRepo(".")), Mock.Of<IToolbox>(), Mock.Of<IAgentMemory>(), CancellationToken.None))
            .Should().Be(AgentActions.None);
        ready.State.Should().Be(AgentState.Completed);
    }



    [Fact]
    public async Task InProcessAgentHandle_exposes_container_operations()
    {
        var container = CreateGenericContainer("handle-1");
        await container.InitializeAsync();
        var handle = new InProcessAgentHandle(container);

        handle.AgentId.Should().Be("handle-1");
        handle.State.Should().Be(AgentState.Ready);
        (await handle.ExecuteAsync()).Should().NotBeNull();
        await handle.ShutdownAsync();
        handle.State.Should().Be(AgentState.Terminated);
        handle.Terminate();
    }




    private static AgentContainer CreateGenericContainer(string agentId, string goal = "work")
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var factory = new AgentFactory(sp.GetRequiredService<ILogger<AgentFactory>>(), sp);
        return factory.CreateContainer(new AgentSpawnSpec
        {
            AgentId = agentId,
            Domain = "Planning",
            Goal = goal,
        });
    }

    private static void SetAgentState(BaseAgent agent, AgentState state)
    {
        var field = typeof(BaseAgent).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(agent, state);
    }
    /// <summary>Failing shutdown agent.</summary>
    private sealed class FailingShutdownAgent : BaseAgent
    {
        public FailingShutdownAgent(AgentSpawnSpec spec, ILogger<BaseAgent> logger) : base(spec, logger) { }

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
        protected override Task<object> OnExecuteAsync(IReadOnlyDictionary<string, object>? dependencyOutputs, CancellationToken cancellationToken) => Task.FromResult<object>("ok");
        /// <summary>On shutdown async.</summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task OnShutdownAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("shutdown failed");
    }

    /// <summary>Failing execute agent.</summary>
    private sealed class FailingExecuteAgent : BaseAgent
    {
        public FailingExecuteAgent(AgentSpawnSpec spec, ILogger<BaseAgent> logger) : base(spec, logger) { }

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
            /// <param name="failed"">Failed".</param>
            throw new InvalidOperationException("execute failed");
        /// <summary>On shutdown async.</summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task OnShutdownAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
