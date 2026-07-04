namespace Nexo.Core.Application.Environments;

/// <summary>
/// One issue found during map data verification (topology, hydrology, routing).
/// </summary>
/// <param name="Category">Issue category (see <see cref="MapVerificationCategories"/>).</param>
/// <param name="Severity">Severity level (see <see cref="MapVerificationSeverity"/>).</param>
/// <param name="Message">Human-readable description of the issue.</param>
/// <param name="Evidence">Optional key-value evidence supporting the finding.</param>
public sealed record MapVerificationIssue(
    string Category,
    string Severity,
    string Message,
    IReadOnlyDictionary<string, string>? Evidence = null);
