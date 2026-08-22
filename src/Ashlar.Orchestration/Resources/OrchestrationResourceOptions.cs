using Ashlar.Abstractions.Resources;

namespace Ashlar.Orchestration.Resources;

/// <summary>
/// Configuration for <see cref="OrchestrationResourceScope"/> (bind section <c>Orchestration:Resources</c> or similar).
/// </summary>
public sealed class OrchestrationResourceOptions
{
    public const string SectionName = "Orchestration:Resources";

    /// <summary>
    /// Order in which to dispose tracked resources when both agents and databases are present.
    /// </summary>
    public ResourceTeardownPolicy TeardownPolicy { get; set; } = ResourceTeardownPolicy.AgentsBeforeDatabases;
}
