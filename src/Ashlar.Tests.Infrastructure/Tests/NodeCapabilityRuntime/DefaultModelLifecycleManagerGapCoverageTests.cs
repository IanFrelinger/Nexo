using Ashlar.Tests.Infrastructure.Helpers;
using FluentAssertions;
using Moq;
using Ashlar.Core.Application.NodeCapabilityRuntime.Models;
using Ashlar.Core.Application.NodeCapabilityRuntime.Ports;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Lifecycle;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

/// <summary>Tests for default model lifecycle manager gap coverage.</summary>
public class DefaultModelLifecycleManagerGapCoverageTests
{
    private static ModelDescriptor Model => new()
    {
        Id = "test-model",
        Provider = "ollama",
        ProviderModelId = "test-model",
    };

    [Fact]
    public void Constructor_rejects_null_backend()
    {
        var act = () => new DefaultModelLifecycleManager(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task EnsureLoadedAsync_loads_when_model_not_present()
    {
        var backend = new Mock<IModelServingBackend>();
        backend.Setup(b => b.ListLoadedModelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        backend.Setup(b => b.LoadModelAsync(Model.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = new DefaultModelLifecycleManager(backend.Object);
        var loaded = await manager.EnsureLoadedAsync(Model);

        loaded.Should().BeTrue();
        backend.Verify(b => b.LoadModelAsync(Model.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureLoadedAsync_skips_load_when_model_already_loaded()
    {
        var backend = new Mock<IModelServingBackend>();
        backend.Setup(b => b.ListLoadedModelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "TEST-MODEL" });

        var manager = new DefaultModelLifecycleManager(backend.Object);
        await manager.EnsureLoadedAsync(Model);

        backend.Verify(b => b.LoadModelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnloadAsync_evictAsync_and_pullAsync_delegate_to_backend()
    {
        var backend = new Mock<IModelServingBackend>();
        backend.Setup(b => b.UnloadModelAsync(Model.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        backend.Setup(b => b.PullModelAsync(Model.Id, It.IsAny<IProgress<PullProgress>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = new DefaultModelLifecycleManager(backend.Object);
        var progress = new SyncProgress<PullProgress>();

        await manager.UnloadAsync(Model);
        await manager.EvictAsync(Model);
        await manager.PullAsync(Model, progress);

        backend.Verify(b => b.UnloadModelAsync(Model.Id, It.IsAny<CancellationToken>()), Times.Exactly(2));
        backend.Verify(
            b => b.PullModelAsync(Model.Id, progress, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunIdleMaintenanceAsync_completes_or_honors_cancellation()
    {
        var manager = new DefaultModelLifecycleManager(new Mock<IModelServingBackend>().Object);

        await manager.RunIdleMaintenanceAsync();

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var act = () => manager.RunIdleMaintenanceAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
