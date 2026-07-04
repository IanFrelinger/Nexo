namespace Nexo.Core.Application.Common.Ports;

/// <summary>Outcome of a loop or select operation.</summary>
public sealed record LoopResult
{
    /// <summary>True when all items were processed without early break or bounds.</summary>
    public bool Completed { get; init; }

    /// <summary>True when cancellation was requested.</summary>
    public bool Cancelled { get; init; }

    /// <summary>True when the time budget was exceeded.</summary>
    public bool TimeBudgetExceeded { get; init; }

    /// <summary>Number of iterations executed.</summary>
    public int Iterations { get; init; }
}
