namespace Nexo.Core.Application.Common.Ports;

/// <summary>
/// Abstraction for bounded iteration over collections used by brick loops and validators.
/// Composed via decorator chain (sequential, parallel, instrumented) at host registration time.
/// </summary>
public interface ILoopKernel
{
    /// <summary>Synchronously iterates items, invoking <paramref name="body"/> for each.</summary>
    LoopResult ForEach<T>(
        IEnumerable<T> items,
        Func<T, int, CancellationToken, LoopAction> body,
        LoopOptions? options,
        CancellationToken ct);

    /// <summary>Asynchronously iterates items, awaiting <paramref name="body"/> for each.</summary>
    ValueTask<LoopResult> ForEachAsync<T>(
        IEnumerable<T> items,
        Func<T, int, CancellationToken, ValueTask<LoopAction>> body,
        LoopOptions? options,
        CancellationToken ct);

    /// <summary>Maps items to outputs, returning a list of produced values.</summary>
    IReadOnlyList<TOut> SelectToList<TIn, TOut>(
        IEnumerable<TIn> items,
        Func<TIn, int, CancellationToken, TOut> map,
        LoopOptions? options,
        CancellationToken ct);
}
