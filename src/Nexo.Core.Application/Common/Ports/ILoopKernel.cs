namespace Nexo.Core.Application.Common.Ports;

public enum LoopAction
{
    Continue = 0,
    Break = 1
}

public sealed record LoopOptions
{
    public string? Name { get; init; }
    public int? MaxIterations { get; init; }
    public TimeSpan? TimeBudget { get; init; }
    public bool EnableParallel { get; init; }
    public int? MaxDegreeOfParallelism { get; init; }
}

public sealed record LoopResult
{
    public bool Completed { get; init; }
    public bool Cancelled { get; init; }
    public bool TimeBudgetExceeded { get; init; }
    public int Iterations { get; init; }
}

public interface ILoopKernel
{
    LoopResult ForEach<T>(
        IEnumerable<T> items,
        Func<T, int, CancellationToken, LoopAction> body,
        LoopOptions? options,
        CancellationToken ct);

    ValueTask<LoopResult> ForEachAsync<T>(
        IEnumerable<T> items,
        Func<T, int, CancellationToken, ValueTask<LoopAction>> body,
        LoopOptions? options,
        CancellationToken ct);

    IReadOnlyList<TOut> SelectToList<TIn, TOut>(
        IEnumerable<TIn> items,
        Func<TIn, int, CancellationToken, TOut> map,
        LoopOptions? options,
        CancellationToken ct);
}

