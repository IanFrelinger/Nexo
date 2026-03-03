namespace Nexo.Core.Application.SelfContext.Models;

/// <summary>
/// Record of a test failure. Used to trigger adaptation (Phase F).
/// </summary>
public record TestFailureRecord
{
    public required string Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string TestName { get; init; }
    public string? FilePath { get; init; }
    public string? ErrorMessage { get; init; }
    public string? StackTrace { get; init; }
}
