using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// A Processing Brick is an atomic unit of domain logic with swappable implementations.
/// </summary>
public abstract class Brick
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Version { get; init; } = "1.0.0";
    public string Icon { get; init; } = "📦";
    public BrickCategory Category { get; init; }
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
    public abstract Task<BrickOutput> ExecuteAsync(
        BrickInput input, 
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default);
}

