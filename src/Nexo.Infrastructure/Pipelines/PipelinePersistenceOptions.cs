namespace Nexo.Infrastructure.Pipelines;

/// <summary>
/// Persistence strategy for pipeline runs.
/// </summary>
public sealed class PipelinePersistenceOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Nexo:Pipelines:Persistence";

    /// <summary>
    /// Provider name: InMemory or LiteDb.
    /// </summary>
    public string Provider { get; set; } = "InMemory";

    /// <summary>
    /// Path used by durable providers.
    /// </summary>
    public string DatabasePath { get; set; } = "nexo-pipeline-runs.db";
}
