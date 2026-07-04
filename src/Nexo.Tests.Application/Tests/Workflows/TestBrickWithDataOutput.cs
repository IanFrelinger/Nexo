using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Workflows;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Core.Domain.Workflows;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Infrastructure.Workflows;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Workflows;

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
