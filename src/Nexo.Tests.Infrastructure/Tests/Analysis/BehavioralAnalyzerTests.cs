using FluentAssertions;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Analysis.BrickAnalyzer;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Analysis;

/// <summary>Tests for behavioral analyzer.</summary>
public class BehavioralAnalyzerTests
{
    private readonly IBehavioralAnalyzer _analyzer = new BehavioralAnalyzer();

    [Fact]
    public async Task AnalyzeAsync_OutputMatchesContract_ContractSatisfied()
    {
        var brick = new TestBrick();
        var output = new BrickOutput();
        output.Set("result", "ok");

        var result = await _analyzer.AnalyzeAsync(brick, output);

        result.ContractSatisfied.Should().BeTrue();
        result.DriftDescriptions.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_MissingOutput_ReportsDrift()
    {
        var brick = new TestBrick();
        var output = new BrickOutput();

        var result = await _analyzer.AnalyzeAsync(brick, output);

        result.ContractSatisfied.Should().BeFalse();
        result.DriftDescriptions.Should().Contain(d => d.Contains("result"));
    }

    /// <summary>Tests for test brick.</summary>
    private sealed class TestBrick : DomainBrick
    {
        public TestBrick()
        {
            Id = "test";
            Name = "Test";
            Interface = new BrickInterface
            {
                Outputs = [new BrickOutputDefinition("result", "string", "The result")],
            };
        }

        public override Task<BrickOutput> ExecuteAsync(BrickInput input, ImplementationType implementation, IExecutionContext context, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
