using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Infrastructure.Pipelines;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Pipelines;

public class InfrastructurePipelinesGapCoverageTests
{
    [Fact]
    public async Task DefaultAgenticStageExecutionAdapter_executes_stage()
    {
        var adapter = new DefaultAgenticStageExecutionAdapter(
            NullLogger<DefaultAgenticStageExecutionAdapter>.Instance);

        adapter.AdapterKey.Should().Be("default");
        adapter.WorkerType.Should().Be(PipelineWorkerType.Agentic);

        var result = await adapter.ExecuteAsync(SampleRequest("stage-a"), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
        result.WorkerId.Should().Be("agentic-default");
        result.Output.Should().Contain("agentic:stage-a:ok");
    }

    [Fact]
    public async Task DefaultDeterministicStageExecutionAdapter_executes_stage()
    {
        var adapter = new DefaultDeterministicStageExecutionAdapter(
            NullLogger<DefaultDeterministicStageExecutionAdapter>.Instance);

        adapter.WorkerType.Should().Be(PipelineWorkerType.Deterministic);

        var result = await adapter.ExecuteAsync(SampleRequest("stage-d"), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
        result.Output.Should().Contain("deterministic:stage-d:ok");
    }

    [Fact]
    public async Task Pipeline_adapters_reject_null_request_and_honor_cancellation()
    {
        var agentic = new DefaultAgenticStageExecutionAdapter(
            NullLogger<DefaultAgenticStageExecutionAdapter>.Instance);
        var deterministic = new DefaultDeterministicStageExecutionAdapter(
            NullLogger<DefaultDeterministicStageExecutionAdapter>.Instance);

        var act = () => agentic.ExecuteAsync(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var act2 = () => deterministic.ExecuteAsync(SampleRequest("x"), cts.Token);
        await act2.Should().ThrowAsync<OperationCanceledException>();
    }

    private static PipelineStageExecutionRequest SampleRequest(string stageId)
    {
        var stage = new PipelineStageDefinition { Id = stageId, Name = stageId };
        var node = new PipelineExecutionNode { StageId = stageId, Mode = PipelineExecutionMode.Deterministic };
        return new PipelineStageExecutionRequest
        {
            RunId = "run-1",
            StageId = stageId,
            Attempt = 1,
            WorkerType = PipelineWorkerType.Deterministic,
            Stage = stage,
            Node = node,
        };
    }
}
