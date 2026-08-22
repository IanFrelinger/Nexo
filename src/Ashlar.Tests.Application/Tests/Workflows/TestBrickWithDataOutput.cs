using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.Core.Application.Workflows;
using Ashlar.Core.Domain.Agents;
using Ashlar.Core.Domain.Behaviors;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Clusters;
using Ashlar.Core.Domain.Execution;
using Ashlar.Core.Domain.Execution.Events;
using Ashlar.Core.Domain.Workflows;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Core.Application.Common.Services;
using Ashlar.Infrastructure.Workflows;
using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.Application.Tests.Workflows;

/// <summary>
/// Test brick that outputs data for output format tests.
/// </summary>
public class TestBrickWithDataOutput : DomainBrick
{
    public TestBrickWithDataOutput()
    {
        Id = "data-brick";
        Name = "Data DomainBrick";
        Category = BrickCategory.Analysis;
        Description = "Outputs data for format tests";
        Interface = new BrickInterface();
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "data-det",
                Name = "Data",
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
        var output = new BrickOutput { Summary = "Data" };
        output["result"] = new Dictionary<string, object>
        {
            ["items"] = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["id"] = 1, ["name"] = "a" },
                new Dictionary<string, object> { ["id"] = 2, ["name"] = "b" }
            }
        };
        return output;
    }
}
