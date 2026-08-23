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
/// Test brick for workflow executor tests.
/// </summary>
public class TestBrickForWorkflow : DomainBrick
{
    public TestBrickForWorkflow()
    {
        Id = "test-brick";
        Name = "Test DomainBrick";
        Category = BrickCategory.Analysis;
        Description = "Test";
        
        Interface = new BrickInterface();
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "test-det",
                Name = "Test",
                Description = "Test",
                Executor = "Test",
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = true,
                    RequiresNetwork = false
                }
            },
            Agentic = new AgenticImplementation
            {
                Id = "test-agentic",
                Name = "Test Agentic",
                Description = "Always throws to force fallback in tests"
            }
        };
        
        DefaultImplementation = ImplementationType.Agentic;
        FallbackChain = new[] { ImplementationType.Agentic, ImplementationType.Deterministic };
    }
    
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        if (implementation == ImplementationType.Agentic)
        {
            throw new InvalidOperationException("Agentic implementation failed (test)");
        }
        var output = new BrickOutput
        {
            Summary = "DomainBrick executed"
        };
        output["result"] = "brick-output";
        return output;
    }
}
