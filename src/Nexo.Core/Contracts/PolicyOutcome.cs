using System.Collections.Generic;

namespace Nexo.Core.Contracts
{
    /// <summary>
    /// Represents the outcome of a policy evaluation.
    /// </summary>
    /// <param name="Passed">Whether the policy evaluation passed</param>
    /// <param name="PolicyPackName">The name of the policy pack used</param>
    /// <param name="Findings">List of findings from the policy evaluation</param>
    /// <param name="Score">The quality score (0.0 to 1.0)</param>
    public record PolicyOutcome(bool Passed, string PolicyPackName, IReadOnlyList<string> Findings, double Score);
}
