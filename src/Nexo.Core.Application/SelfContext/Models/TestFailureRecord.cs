namespace Nexo.Core.Application.SelfContext.Models;

/// <summary>Record of a test failure. Used to trigger adaptation (Phase F).</summary>
public record TestFailureRecord
{
    /// <summary>Stable failure record identifier.</summary>
    public required string Id { get; init; }

    /// <summary>UTC timestamp when the failure was recorded.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Full name of the failing test.</summary>
    public required string TestName { get; init; }

    /// <summary>Source file path associated with the failure, when available.</summary>
    public string? FilePath { get; init; }

    /// <summary>Failure message from the test runner.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Stack trace captured on failure, when available.</summary>
    public string? StackTrace { get; init; }
}
