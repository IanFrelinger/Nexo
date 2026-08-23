using Ashlar.Core.Application.Common.Models;

namespace Ashlar.Infrastructure.Validation.Parsers;

/// <summary>
/// Port for parsing test result files (TRX, JUnit XML, etc.).
/// 
/// Defines the contract for parsers that extract test results from various file formats.
/// Each parser supports specific file extensions (e.g., .trx for Visual Studio test results).
/// 
/// Implementations (e.g., TrxTestResultParser) provide format-specific parsing logic.
/// Used by ValidationServiceAdapter to parse test execution results.
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

