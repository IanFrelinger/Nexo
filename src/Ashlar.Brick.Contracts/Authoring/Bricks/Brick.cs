using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Domain.Bricks;

/// <summary>
/// A Processing Brick is an atomic unit of domain logic with swappable implementations.
/// </summary>
public abstract class Brick
{
    /// <summary>Stable brick identifier used in catalog and execute routes.</summary>
    public string Id { get; init; } = default!;

    /// <summary>Human-readable display name shown in operator UIs.</summary>
    public string Name { get; init; } = default!;

    /// <summary>Semantic version of the brick implementation.</summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>Emoji or icon token for catalog and workflow composer UIs.</summary>
    public string Icon { get; init; } = "📦";

    /// <summary>Primary functional category for catalog grouping.</summary>
    public BrickCategory Category { get; init; }

    /// <summary>Short description of what the brick does.</summary>
    public string Description { get; init; } = default!;
    
    /// <summary>
    /// Domain knowledge encapsulated by this brick.
    /// </summary>
    public DomainKnowledge DomainKnowledge { get; init; } = new();
    
    /// <summary>
    /// Interface contract for this brick.
    /// </summary>
    public BrickInterface Interface { get; init; } = new();
    
    /// <summary>
    /// Available implementations (deterministic and/or agentic).
    /// </summary>
    public BrickImplementations Implementations { get; init; } = new();
    
    /// <summary>
    /// Default implementation to use.
    /// </summary>
    public ImplementationType DefaultImplementation { get; init; } = ImplementationType.Deterministic;
    
    /// <summary>
    /// Fallback chain when preferred implementation unavailable.
    /// </summary>
    public IReadOnlyList<ImplementationType> FallbackChain { get; init; } = 
        [ImplementationType.Deterministic, ImplementationType.Agentic];
    
    /// <summary>
    /// Optional selector logic for choosing implementation at runtime.
    /// </summary>
    public ImplementationSelector? Selector { get; init; }
    
    /// <summary>
    /// Metadata about this brick.
    /// </summary>
    public BrickMetadata Metadata { get; init; } = new();
    
    /// <summary>
    /// Execute this brick with the given input and implementation type.
    /// </summary>
    /// <param name="input">Named input values matching <see cref="Interface"/>.</param>
    /// <param name="implementation">Deterministic, agentic, or auto-selected implementation.</param>
    /// <param name="context">Runtime environment and workflow variables.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>Named output values and optional summary text.</returns>
    public abstract Task<BrickOutput> ExecuteAsync(
        BrickInput input, 
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default);
}
