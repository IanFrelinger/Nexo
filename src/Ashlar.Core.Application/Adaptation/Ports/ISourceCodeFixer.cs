using Ashlar.Core.Application.Analysis.Models;

namespace Ashlar.Core.Application.Adaptation.Ports;

/// <summary>
/// Applies source-level fixes for analysis violations.
/// Block 4: closes the loop by modifying .cs files.
/// </summary>
public interface ISourceCodeFixer
{
    /// <summary>
    /// Attempts to fix the given violation in the source file.
    /// </summary>
    /// <param name="violation">The violation to fix.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file was modified.</returns>
    Task<bool> TryFixAsync(Violation violation, CancellationToken cancellationToken = default);
}
