using FluentAssertions;
using Moq;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution.Agentic;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

/// <summary>Tests for ncr agentic brick engine.</summary>
public sealed class NcrAgenticBrickEngineTests
{
    [Fact]
    public async Task ResolveModelForBrickAsync_MapsContext_AndEnsuresModelReady()
    {
        var brick = new TestBrick
        {
            Id = "test",
            Name = "test",
            Category = BrickCategory.Security,
            Description = "desc",
            Implementations = new BrickImplementations { Agentic = new AgenticImplementation { Id = "a", Name = "a", Description = "a" } }
        };
        var context = new Nexo.Infrastructure.Execution.ExecutionContext
        {
            AgentId = "a1",
            BehaviorId = "b1",
            Provider = "ollama",
            IsAirGapped = false,
            AuditMode = false,
            Variables = new Dictionary<string, object>
            {
                ["nexo:user_facing"] = true,
                ["nexo:quality"] = "High"
            }
        };

        var expectedModel = new ModelDescriptor { Id = "qwen", Provider = "ollama", Capabilities = new HashSet<TaskCapability> { TaskCapability.CodeGeneration } };
        var ncr = new Mock<INodeCapabilityRuntime>(MockBehavior.Strict);
        ncr.Setup(x => x.SelectModelAsync(
                It.Is<TaskContext>(ctx =>
                    ctx.RequiredCapability == TaskCapability.CodeGeneration &&
                    ctx.Quality == QualityRequirement.High &&
                    ctx.Latency == LatencyRequirement.Interactive &&
                    ctx.Privacy == PrivacyBoundary.Standard &&
                    ctx.IsUserFacing &&
                    ctx.RequestedProvider == "ollama"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelResolution
            {
                Model = expectedModel,
                Target = InferenceTarget.Local,
                Reason = ResolutionReason.BestLocalFit
            });
        ncr.Setup(x => x.EnsureModelReadyAsync(expectedModel, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new NcrAgenticBrickEngine(ncr.Object);

        var resolution = await sut.ResolveModelForBrickAsync(brick, context, CancellationToken.None);

        resolution.Target.Should().Be(InferenceTarget.Local);
        resolution.Model.Id.Should().Be("qwen");
        ncr.VerifyAll();
    }

    /// <summary>Tests for test brick.</summary>
    private sealed class TestBrick : DomainBrick
    {
        public override Task<BrickOutput> ExecuteAsync(BrickInput input, ImplementationType implementation, IExecutionContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new BrickOutput());
    }
}
