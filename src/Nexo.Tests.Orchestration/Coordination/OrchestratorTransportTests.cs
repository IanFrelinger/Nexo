using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Abstractions.Transport;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Communication;
using Nexo.Orchestration.Coordination;
using Nexo.Orchestration.Coordination.Conflicts;
using Nexo.Orchestration.Metrics;
using Xunit;

namespace Nexo.Tests.Orchestration.Coordination;

public sealed class OrchestratorTransportTests
{
    [Fact]
    public async Task OrchestrateAsync_WhenTransportReturnsTimeout_RetriesAndSucceeds()
    {
        var transportMock = new Mock<IAgentTransport>(MockBehavior.Strict);
        transportMock
            .SetupSequence(t => t.SendAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult(
                Success: false,
                ErrorMessage: "Timed out",
                Metadata: new Dictionary<string, string> { ["errorCode"] = "TIMEOUT" }))
            .ReturnsAsync(new AgentResult(
                Success: true,
                Output: new { ok = true }));

        var sut = CreateOrchestrator(
            CreateArchitectMock(CreateSingleAgentDecomposition()).Object,
            transportMock.Object);

        var result = await sut.OrchestrateAsync("timeout retry");

        result.Success.Should().BeTrue();
        result.Escalations.Should().BeEmpty();
        transportMock.Verify(
            t => t.SendAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task OrchestrateAsync_WhenTransportReturnsAgentNotFound_EscalatesWithoutRetry()
    {
        var transportMock = new Mock<IAgentTransport>(MockBehavior.Strict);
        transportMock
            .Setup(t => t.SendAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult(
                Success: false,
                ErrorMessage: "Agent not found",
                Metadata: new Dictionary<string, string> { ["errorCode"] = "AGENT_NOT_FOUND" }));

        var sut = CreateOrchestrator(
            CreateArchitectMock(CreateSingleAgentDecomposition()).Object,
            transportMock.Object);

        var result = await sut.OrchestrateAsync("agent not found");

        result.Success.Should().BeFalse();
        result.Escalations.Should().ContainSingle(e => e.IssueType == "AgentExecution");
        transportMock.Verify(
            t => t.SendAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OrchestrateAsync_WhenTransportReturnsUnknownError_RecordsCircuitBreakerFailures()
    {
        var transportMock = new Mock<IAgentTransport>(MockBehavior.Strict);
        transportMock
            .Setup(t => t.SendAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult(
                Success: false,
                ErrorMessage: "Provider failure",
                Metadata: new Dictionary<string, string> { ["errorCode"] = "PROVIDER_FAILURE" }));

        var sut = CreateOrchestrator(
            CreateArchitectMock(CreateSingleAgentDecomposition()).Object,
            transportMock.Object);

        await sut.OrchestrateAsync("unknown error 1");
        await sut.OrchestrateAsync("unknown error 2");
        await sut.OrchestrateAsync("unknown error 3");

        transportMock.Verify(
            t => t.SendAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task OrchestrateAsync_PropagatesCorrelationIdToTransportRequest()
    {
        AgentInvocationRequest? capturedRequest = null;
        var transportMock = new Mock<IAgentTransport>(MockBehavior.Strict);
        transportMock
            .Setup(t => t.SendAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AgentInvocationRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new AgentResult(
                Success: true,
                Output: new { ok = true }));

        var sut = CreateOrchestrator(
            CreateArchitectMock(CreateSingleAgentDecomposition()).Object,
            transportMock.Object);

        var result = await sut.OrchestrateAsync("correlation");

        result.CorrelationId.Should().NotBeNullOrWhiteSpace();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.CorrelationId.Should().Be(result.CorrelationId);
    }

    private static Mock<IArchitectAgent> CreateArchitectMock(DecompositionResult decomposition)
    {
        var architectMock = new Mock<IArchitectAgent>(MockBehavior.Strict);
        architectMock
            .Setup(a => a.DecomposeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(decomposition);
        architectMock
            .Setup(a => a.DecomposeAsync(It.IsAny<string>(), It.IsAny<DecompositionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(decomposition);
        return architectMock;
    }

    private static DecompositionResult CreateSingleAgentDecomposition()
    {
        return new DecompositionResult
        {
            OriginalRequest = "request",
            Agents =
            [
                new AgentSpawnSpec
                {
                    AgentId = "agent-1",
                    Domain = "General",
                    Goal = "Test goal"
                }
            ]
        };
    }

    private static Orchestrator CreateOrchestrator(IArchitectAgent architect, IAgentTransport transport)
    {
        var provider = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        var agentFactory = new AgentFactory(
            provider.GetRequiredService<ILogger<AgentFactory>>(),
            provider);
        var lifecycleManager = new LifecycleManager(
            provider.GetRequiredService<ILogger<LifecycleManager>>(),
            new HealthMonitor(provider.GetRequiredService<ILogger<HealthMonitor>>()));
        var dependencyResolver = new DependencyResolver(
            provider.GetRequiredService<ILogger<DependencyResolver>>());
        var conflictDetector = new ConflictDetector(
            provider.GetRequiredService<ILogger<ConflictDetector>>());
        var resourceAllocator = new ResourceAllocator(
            provider.GetRequiredService<ILogger<ResourceAllocator>>());
        var progressTracker = new ProgressTracker(
            provider.GetRequiredService<ILogger<ProgressTracker>>());
        var escalationManager = new EscalationManager(
            provider.GetRequiredService<ILogger<EscalationManager>>());
        var outputIntegrator = new OutputIntegrator(
            provider.GetRequiredService<ILogger<OutputIntegrator>>());
        var agentBus = new AgentBus(
            provider.GetRequiredService<ILogger<AgentBus>>());
        var loops = new SequentialLoopKernel();
        var metrics = new OrchestrationMetrics(
            provider.GetRequiredService<ILogger<OrchestrationMetrics>>());

        return new Orchestrator(
            architect,
            agentFactory,
            lifecycleManager,
            dependencyResolver,
            conflictDetector,
            resourceAllocator,
            progressTracker,
            escalationManager,
            outputIntegrator,
            agentBus,
            transport,
            loops,
            provider.GetRequiredService<ILogger<Orchestrator>>(),
            metrics: metrics);
    }
}
