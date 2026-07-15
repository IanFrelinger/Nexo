using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.GameDomain.Bricks;

/// <summary>
/// Base for gameplay bricks that only support the deterministic implementation path.
/// </summary>
public abstract class DeterministicGameplayBrick : DomainBrick
{
    /// <summary>Configures deterministic-only defaults.</summary>
    protected DeterministicGameplayBrick()
    {
        DefaultImplementation = ImplementationType.Deterministic;
        FallbackChain = new[] { ImplementationType.Deterministic };
        Selector = new ImplementationSelector
        {
            PreferDeterministic =
            [
                "environment.airGapped",
                "context.auditMode"
            ],
            Default = ImplementationType.Deterministic
        };
    }

    /// <inheritdoc />
    public sealed override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (implementation == ImplementationType.Agentic)
        {
            throw new InvalidOperationException(
                $"Brick '{Id}' is deterministic-only; agentic execution is not supported in the gameplay host.");
        }

        var resolved = implementation == ImplementationType.Auto
            ? ImplementationType.Deterministic
            : implementation;

        if (resolved != ImplementationType.Deterministic)
        {
            throw new InvalidOperationException(
                $"Brick '{Id}' cannot execute implementation '{implementation}'.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ExecuteDeterministic(input, context));
    }

    /// <summary>Pure deterministic rule body.</summary>
    protected abstract BrickOutput ExecuteDeterministic(BrickInput input, IExecutionContext context);
}
