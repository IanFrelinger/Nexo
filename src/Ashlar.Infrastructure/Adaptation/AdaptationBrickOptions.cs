namespace Ashlar.Infrastructure.Adaptation;

/// <summary>
/// Options for registering additional bricks in the adaptation pipeline.
/// </summary>
public sealed class AdaptationBrickOptions
{
    /// <summary>
    /// Additional brick types to register. Resolved via <see cref="Microsoft.Extensions.DependencyInjection.ActivatorUtilities"/>.
    /// </summary>
    public List<Type> AdditionalBrickTypes { get; } = new();
}
