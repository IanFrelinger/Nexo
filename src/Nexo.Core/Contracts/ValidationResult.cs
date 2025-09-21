using System.Collections.Generic;

namespace Nexo.Core.Contracts
{
    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    /// <param name="Passed">Whether the validation passed</param>
    /// <param name="Name">The name of the validation gate</param>
    /// <param name="Findings">List of findings from the validation</param>
    public record ValidationResult(bool Passed, string Name, IReadOnlyList<string> Findings);
}
