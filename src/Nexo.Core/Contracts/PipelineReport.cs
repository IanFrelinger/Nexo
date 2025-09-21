using System.Collections.Generic;

namespace Nexo.Core.Contracts
{
    /// <summary>
    /// Represents a report from a pipeline execution.
    /// </summary>
    /// <param name="ArtifactId">The unique identifier of the artifact</param>
    /// <param name="Succeeded">Whether the pipeline execution succeeded</param>
    /// <param name="QualityScore">The overall quality score (0.0 to 1.0)</param>
    /// <param name="Notes">Additional notes about the pipeline execution</param>
    public record PipelineReport(string ArtifactId, bool Succeeded, double QualityScore, IReadOnlyList<string> Notes);
}
