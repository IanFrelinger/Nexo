namespace Nexo.Infrastructure.Pipelines.Sdk.Options;
/// <summary>
/// Declares adapter identifiers used by stage executors.
/// </summary>
public sealed class PipelineExecutionAdapterOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Nexo:Pipelines:Adapters";

    /// <summary>
    /// Adapter key for deterministic stages.
    /// </summary>
    public string DeterministicAdapter { get; set; } = "default";

    /// <summary>
    /// Adapter key for agentic stages.
    /// </summary>
    public string AgenticAdapter { get; set; } = "default";
}
