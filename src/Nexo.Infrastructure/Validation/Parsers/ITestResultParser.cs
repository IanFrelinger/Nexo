using Nexo.Core.Application.Validation.Models;

namespace Nexo.Infrastructure.Validation.Parsers;

/// <summary>
/// Port for parsing test result files (TRX, JUnit XML, etc.).
/// </summary>
public interface ITestResultParser
{
    /// <summary>
    /// Parses a test result file and extracts test results.
    /// </summary>
    Task<IReadOnlyList<TestResult>> ParseAsync(
        FileInfo resultFile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the file extensions this parser supports.
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }
}

