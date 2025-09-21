using System.Diagnostics;

namespace Nexo.Observability.ActivitySources;

/// <summary>
/// Centralized activity sources for Nexo components.
/// </summary>
public static class NexoActivitySources
{
    /// <summary>
    /// Activity source for generation operations.
    /// </summary>
    public static readonly ActivitySource Generation = new("Nexo.Generation");

    /// <summary>
    /// Activity source for validation operations.
    /// </summary>
    public static readonly ActivitySource Validation = new("Nexo.Validation");

    /// <summary>
    /// Activity source for policy operations.
    /// </summary>
    public static readonly ActivitySource Policy = new("Nexo.Policy");

    /// <summary>
    /// Activity source for pipeline operations.
    /// </summary>
    public static readonly ActivitySource Pipeline = new("Nexo.Pipeline");

    /// <summary>
    /// Activity source for repair operations.
    /// </summary>
    public static readonly ActivitySource Repair = new("Nexo.Repair");

    /// <summary>
    /// Activity source for observability operations.
    /// </summary>
    public static readonly ActivitySource Observability = new("Nexo.Observability");

    /// <summary>
    /// Gets all activity sources.
    /// </summary>
    /// <returns>Array of all activity sources.</returns>
    public static ActivitySource[] GetAll()
    {
        return new[]
        {
            Generation,
            Validation,
            Policy,
            Pipeline,
            Repair,
            Observability
        };
    }
}
