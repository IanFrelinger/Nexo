namespace Nexo.Contracts;

/// <summary>Aggregated validation test results.</summary>
/// <param name="Passed">True when all executed tests passed.</param>
/// <param name="Message">Summary message for operators.</param>
/// <param name="TotalTests">Total tests executed.</param>
/// <param name="PassedTests">Number of tests that passed.</param>
/// <param name="FailedTests">Number of tests that failed.</param>
public sealed record ValidationResponse(bool Passed, string? Message, int TotalTests, int PassedTests, int FailedTests);
