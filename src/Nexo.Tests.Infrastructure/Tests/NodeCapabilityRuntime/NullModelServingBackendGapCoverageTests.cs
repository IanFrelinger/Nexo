using FluentAssertions;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Infrastructure.NodeCapabilityRuntime.Backends;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

/// <summary>Tests for null model serving backend gap coverage.</summary>
public class NullModelServingBackendGapCoverageTests
{
    [Fact]
    public async Task NullModelServingBackend_supports_full_lifecycle()
    {
        var backend = new NullModelServingBackend();
        backend.BackendType.Should().Be(BackendType.OnnxRuntime);

        (await backend.IsAvailableAsync()).Should().BeTrue();

        var progressReports = new List<PullProgress>();
        await backend.PullModelAsync("m1", new Progress<PullProgress>(progressReports.Add));

        progressReports.Should().ContainSingle(p => p.ModelId == "m1" && p.TotalBytes == 1);
        (await backend.ListLoadedModelsAsync()).Should().BeEmpty();

        await backend.LoadModelAsync("m1");
        (await backend.ListLoadedModelsAsync()).Should().ContainSingle("m1");

        var inference = await backend.RunInferenceAsync(new InferenceRequest { ModelId = "m1", Prompt = "hi" });
        inference.Output.Should().Contain("m1");

        await backend.UnloadModelAsync("m1");
        (await backend.ListLoadedModelsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task NullModelServingBackend_honors_cancellation()
    {
        var backend = new NullModelServingBackend();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var actAvailable = () => backend.IsAvailableAsync(cts.Token);
        var actInference = () => backend.RunInferenceAsync(new InferenceRequest { ModelId = "m1" }, cts.Token);
        var actLoad = () => backend.LoadModelAsync("m1", cts.Token);
        var actUnload = () => backend.UnloadModelAsync("m1", cts.Token);
        var actPull = () => backend.PullModelAsync("m1", progress: null, ct: cts.Token);
        var actList = () => backend.ListLoadedModelsAsync(cts.Token);

        await actAvailable.Should().ThrowAsync<OperationCanceledException>();
        await actInference.Should().ThrowAsync<OperationCanceledException>();
        await actLoad.Should().ThrowAsync<OperationCanceledException>();
        await actUnload.Should().ThrowAsync<OperationCanceledException>();
        await actPull.Should().ThrowAsync<OperationCanceledException>();
        await actList.Should().ThrowAsync<OperationCanceledException>();
    }
}
