using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GameDomain.Bricks;

namespace Nexo.GameDomain;

/// <summary>
/// Executes gameplay bricks on the deterministic path only.
/// Resolves <see cref="ImplementationType.Auto"/> via <see cref="ImplementationSelector"/> when present,
/// then forces Deterministic (agentic throws).
/// </summary>
public sealed class DeterministicGameplayRunner
{
    private readonly GameplayBrickRegistry _registry;

    /// <summary>Creates a runner bound to a registry.</summary>
    public DeterministicGameplayRunner(GameplayBrickRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>Looks up <paramref name="brickId"/> and executes it deterministically.</summary>
    public Task<BrickOutput> ExecuteAsync(
        string brickId,
        BrickInput input,
        IExecutionContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(brickId))
            throw new ArgumentException("Brick id is required.", nameof(brickId));
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        var brick = _registry.GetBrick(brickId)
            ?? throw new KeyNotFoundException($"Gameplay brick '{brickId}' is not registered.");

        return ExecuteAsync(brick, input, context, cancellationToken);
    }

    /// <summary>Executes a brick instance deterministically.</summary>
    public Task<BrickOutput> ExecuteAsync(
        DomainBrick brick,
        BrickInput input,
        IExecutionContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (brick is null)
            throw new ArgumentNullException(nameof(brick));
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        var ctx = context ?? GameplayExecutionContext.ForPlayer("gameplay");
        var requested = ResolveImplementation(brick, ctx);
        if (requested != ImplementationType.Deterministic)
        {
            throw new InvalidOperationException(
                $"Gameplay runner requires Deterministic execution; brick '{brick.Id}' resolved to {requested}.");
        }

        return brick.ExecuteAsync(input, ImplementationType.Deterministic, ctx, cancellationToken);
    }

    private static ImplementationType ResolveImplementation(DomainBrick brick, IExecutionContext context)
    {
        var preferred = brick.DefaultImplementation;
        if (preferred == ImplementationType.Auto && brick.Selector is not null)
            preferred = brick.Selector.Select(context);

        if (preferred == ImplementationType.Auto)
            preferred = ImplementationType.Deterministic;

        // Air-gap / audit gameplay hosts always stay on the rule path.
        if (context.IsAirGapped || context.AuditMode)
            return ImplementationType.Deterministic;

        return preferred;
    }
}
