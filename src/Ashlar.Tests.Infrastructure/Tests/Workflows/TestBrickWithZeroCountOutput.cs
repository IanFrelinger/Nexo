using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Core.Application.Common.Services;
using Ashlar.Core.Application.Workflows;
using Ashlar.Core.Domain.Agents;
using Ashlar.Core.Domain.Behaviors;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Core.Domain.Workflows;
using Ashlar.Tests.Application.Tests.Workflows;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Workflows;

/// <summary>
/// Test brick that outputs data with count=0 for ConditionalNode false case.
/// </summary>
internal sealed class TestBrickWithZeroCountOutput : DomainBrick
{
    public TestBrickWithZeroCountOutput()
    {
        Id = "zero-brick";
        Name = "Zero DomainBrick";
        Category = BrickCategory.Analysis;
        Description = "Outputs count=0";
        Interface = new BrickInterface();
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "zero-det",
                Name = "Zero",
                Description = "Test",
                Executor = "Test",
                Characteristics = new ImplementationCharacteristics { Deterministic = true, RequiresNetwork = false }
            }
        };
        DefaultImplementation = ImplementationType.Deterministic;
        FallbackChain = Array.Empty<ImplementationType>();
    }

    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        var output = new BrickOutput { Summary = "Zero" };
        output["result"] = new Dictionary<string, object> { ["data"] = new Dictionary<string, object> { ["count"] = 0 } };
        return output;
    }
}
