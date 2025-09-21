using System.Collections.Generic;

namespace Nexo.Core.Contracts
{
    /// <summary>
    /// Represents the result of a generation operation.
    /// </summary>
    /// <typeparam name="TArtifact">The type of artifact generated</typeparam>
    /// <param name="Artifact">The generated artifact</param>
    /// <param name="SourceCode">The source code used to generate the artifact</param>
    /// <param name="Notes">Additional notes about the generation process</param>
    public record GenerationResult<TArtifact>(TArtifact Artifact, string SourceCode, IReadOnlyList<string> Notes);
}
