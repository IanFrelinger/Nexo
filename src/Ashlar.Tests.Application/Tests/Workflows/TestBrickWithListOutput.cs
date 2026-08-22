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
/// Test brick that outputs a list of dicts for Transform node tests.
/// </summary>
public class TestBrickWithListOutput : DomainBrick
{
    public TestBrickWithListOutput()
    {
        Id = "list-brick";
        Name = "List DomainBrick";
        Category = BrickCategory.Analysis;
        Description = "Outputs list for transform tests";
        Interface = new BrickInterface();
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "list-det",
                Name = "List",
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
        var output = new BrickOutput { Summary = "List" };
        output["data"] = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object> { ["value"] = 10 },
            new Dictionary<string, object> { ["value"] = 5 }
        };
        return output;
    }
}
