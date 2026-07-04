using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Nexo.API.Endpoints;
using Nexo.Brick.Contracts.Capabilities;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using System.Reflection;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.API;

/// <summary>Tests for capabilities endpoint.</summary>
public sealed class CapabilitiesEndpointTests
{
    [Fact]
    public async Task GetCapabilities_ReturnsManifestDto()
    {
        var runtime = new StubNodeCapabilityRuntime();
        var expected = runtime.GetCapabilityManifest();
        var method = typeof(NexoEndpoints).GetMethod(
            "GetCapabilitiesAsync",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull("the capabilities endpoint handler should exist");

        var task = (Task<IResult>)method!.Invoke(
            null,
            [runtime, CancellationToken.None])!;
        var result = await task;
        var dto = ExtractDtoFromResult(result);

        dto.Should().NotBeNull();
        dto.NodeId.Should().Be(expected.NodeId);
        dto.AcceptingRemoteWork.Should().Be(expected.AcceptingRemoteWork);
        dto.SupportedCapabilities.Should().Contain(TaskCapabilityDto.Embeddings);
    }

    private static NodeCapabilityManifestDto ExtractDtoFromResult(IResult result)
    {
        var resultType = result.GetType();
        var valueProperty = resultType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
        valueProperty.Should().NotBeNull();

        var value = valueProperty!.GetValue(result);
        value.Should().BeOfType<NodeCapabilityManifestDto>();
        return (NodeCapabilityManifestDto)value!;
    }

    /// <summary>Tests for stub node capability runtime.</summary>
    private sealed class StubNodeCapabilityRuntime : INodeCapabilityRuntime
    {
        /// <summary>Current profile.</summary>
        public NodeProfile CurrentProfile { get; } = new()
        {
            Platform = PlatformType.Linux,
            Tier = NodeTier.Micro
        };

        public IReadOnlyList<ModelDescriptor> AvailableModels => [];

        public IObservable<ConstraintUpdate> ConstraintChanges => new NoopObservable<ConstraintUpdate>();

        public Task EnsureModelReadyAsync(ModelDescriptor model, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<NodeTier> GetTierAsync()
            => Task.FromResult(NodeTier.Micro);

        public NodeCapabilityManifest GetCapabilityManifest()
            => new()
            {
                NodeId = "node-123",
                Tier = NodeTier.Micro,
                Platform = PlatformType.Linux,
                HotModelIds = ["nomic-embed-text"],
                AvailableModelIds = ["nomic-embed-text", "phi3-mini"],
                SupportedCapabilities = [TaskCapability.Embeddings],
                AcceptingRemoteWork = true,
                GeneratedAt = DateTimeOffset.UtcNow
            };

        public Task RecordOutcomeAsync(ModelResolution resolution, Nexo.Core.Application.Execution.Ports.BrickExecutionOutcome outcome, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<ModelResolution> SelectModelAsync(TaskContext context, CancellationToken ct = default)
            => Task.FromResult(new ModelResolution
            {
                Target = InferenceTarget.Local,
                Reason = ResolutionReason.BestLocalFit,
                Model = new ModelDescriptor { Id = "nomic-embed-text" }
            });
    }

    /// <summary>Tests for noop observable.</summary>
    private sealed class NoopObservable<T> : IObservable<T>
    {
        /// <summary>Subscribe.</summary>
        /// <param name="observer">Observer.</param>
        public IDisposable Subscribe(IObserver<T> observer) => new NoopDisposable();

        /// <summary>Tests for noop disposable.</summary>
        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
