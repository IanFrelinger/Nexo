using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Core.Application.Observation.Ports;

namespace Ashlar.Infrastructure.Observation;

/// <summary>
/// DomainBrick that assembles observation context for agents. Wraps IContextAssembler.
/// Dogfood: this is a Ashlar-owned brick that the adaptation engine can improve.
/// </summary>
public sealed class ObservationContextBrick : DomainBrick
{
    private readonly IContextAssembler _assembler;

    /// <summary>Initializes a new observation context brick.</summary>
    public ObservationContextBrick(IContextAssembler assembler)
    {
        _assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));

        Id = "observation.context";
        Name = "Observation Context";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Assembles observation context (patterns, summary) for agent consumption.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("projectPath", "string", "Project path to filter", false),
                new BrickInputDefinition("maxPatterns", "int", "Max patterns to include", false, 50),
            ],
            Outputs =
            [
                new BrickOutputDefinition("context", "ObservationContext", "Assembled observation context"),
            ],
        };
    }

    /// <summary>Execute asynchronously.</summary>
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var projectPath = input.Get<string>("projectPath", null);
        var maxPatterns = input.Get<int>("maxPatterns", 50);

        var observationContext = await _assembler.AssembleContextAsync(
            projectPath: projectPath,
            maxPatterns: maxPatterns,
            includeRecentEvents: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var output = new BrickOutput();
        output.Set("context", observationContext);
        output.Summary = $"Observed {observationContext.Patterns.Count} pattern(s)";
        return output;
    }
}
