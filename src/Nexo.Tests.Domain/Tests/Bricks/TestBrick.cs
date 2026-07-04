using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;

namespace Nexo.Tests.Domain.Tests.Bricks;

/// <summary>
/// Test brick implementation for testing.
/// </summary>
public class TestBrick : DomainBrick
{
    private readonly IProviderFactory _providerFactory;
    
    public TestBrick(IProviderFactory providerFactory, ILogger<TestBrick> logger)
    {
        _providerFactory = providerFactory;
        
        Id = "test-brick";
        Name = "Test DomainBrick";
        Version = "1.0.0";
        Icon = "🧪";
        Category = BrickCategory.Analysis;
        Description = "A test brick";
        
        DomainKnowledge = new DomainKnowledge
        {
            Standards = ["Test Standard"],
            Rules = [new DomainRule("test-rule", "Test rule")]
        };
        
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("input", "string", required: true)],
            Outputs = [new BrickOutputDefinition("output", "string")]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "test-det",
                Name = "Test Deterministic",
                Description = "Test",
                Executor = "TestExecutor",
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = true,
                    RequiresNetwork = false
                }
            }
        };
        
        DefaultImplementation = ImplementationType.Deterministic;
    }
    
    public override async Task<Core.Domain.Execution.BrickOutput> ExecuteAsync(
        Core.Domain.Execution.BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var output = new Core.Domain.Execution.BrickOutput
        {
            ["result"] = "test result",
            Summary = "Test execution completed"
        };
        
        return await Task.FromResult(output);
    }
}
