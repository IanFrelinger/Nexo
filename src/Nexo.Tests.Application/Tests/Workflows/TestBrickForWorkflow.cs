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
