using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Tests.Infrastructure.Certification.Fixtures;

/// <summary>Deterministic summary plus a volatile nonce output for composition determinism tests.</summary>
public sealed class NondeterministicFormatterBrick : DomainBrick
{
    public NondeterministicFormatterBrick()
    {
        Id = "nondeterministic-formatter";
        Name = "Nondeterministic Formatter";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Correct summary with volatile nonce.";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("errorCount", "int", "Error count"),
                new BrickInputDefinition("firstErrorMessage", "string", "First error message")
            ],
            Outputs =
            [
                new BrickOutputDefinition("summary", "string", "Formatted summary"),
                new BrickOutputDefinition("nonce", "int", "Volatile nonce")
            ]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var errorCount = input.Get<int>("errorCount");
        var firstErrorMessage = input.Get<string>("firstErrorMessage") ?? string.Empty;
        var summary = $"Errors={errorCount}; first={firstErrorMessage}";
        var output = new BrickOutput { Summary = summary };
        output.Set("summary", summary);
        output.Set("nonce", Random.Shared.Next());
        return Task.FromResult(output);
    }
}
