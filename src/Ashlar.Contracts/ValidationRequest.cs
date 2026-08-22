namespace Ashlar.Contracts;

/// <summary>Request to run validation tests, optionally filtered by name.</summary>
/// <param name="Filter">Optional test name filter; null runs all tests.</param>
public sealed record ValidationRequest(string? Filter = null);
