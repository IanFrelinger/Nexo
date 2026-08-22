using Ashlar.Core.Application.Common.Ports;

namespace Ashlar.Core.Application.Common.Services;

/// <summary>
/// Baseline sequential implementation of <see cref="ILoopKernel"/>.
/// Honors cancellation, max-iteration, and time-budget options.
/// </summary>
public sealed class SequentialLoopKernel : ILoopKernel
{
    /// <inheritdoc />
    public LoopResult ForEach<T>(
        IEnumerable<T> items,
        Func<T, int, CancellationToken, LoopAction> body,
        LoopOptions? options,
        CancellationToken ct)
    {
        options ??= new LoopOptions();
        var started = DateTimeOffset.UtcNow;
        var i = 0;

        foreach (var item in items)
        {
            if (ct.IsCancellationRequested)
            {
                return new LoopResult { Completed = false, Cancelled = true, Iterations = i };
            }

            if (options.MaxIterations.HasValue && i >= options.MaxIterations.Value)
            {
                return new LoopResult { Completed = false, Iterations = i };
            }

            if (options.TimeBudget.HasValue && DateTimeOffset.UtcNow - started > options.TimeBudget.Value)
            {
                return new LoopResult { Completed = false, TimeBudgetExceeded = true, Iterations = i };
            }

            var action = body(item, i, ct);
            i++;
            if (action == LoopAction.Break)
            {
                return new LoopResult { Completed = false, Iterations = i };
            }
        }

        return new LoopResult { Completed = true, Iterations = i };
    }

    /// <inheritdoc />
    public async ValueTask<LoopResult> ForEachAsync<T>(
        IEnumerable<T> items,
        Func<T, int, CancellationToken, ValueTask<LoopAction>> body,
        LoopOptions? options,
        CancellationToken ct)
    {
        options ??= new LoopOptions();
        var started = DateTimeOffset.UtcNow;
        var i = 0;

        foreach (var item in items)
        {
            if (ct.IsCancellationRequested)
            {
                return new LoopResult { Completed = false, Cancelled = true, Iterations = i };
            }

            if (options.MaxIterations.HasValue && i >= options.MaxIterations.Value)
            {
                return new LoopResult { Completed = false, Iterations = i };
            }

            if (options.TimeBudget.HasValue && DateTimeOffset.UtcNow - started > options.TimeBudget.Value)
            {
                return new LoopResult { Completed = false, TimeBudgetExceeded = true, Iterations = i };
            }

            var action = await body(item, i, ct);
            i++;
            if (action == LoopAction.Break)
            {
                return new LoopResult { Completed = false, Iterations = i };
            }
        }

        return new LoopResult { Completed = true, Iterations = i };
    }

    /// <inheritdoc />
    public IReadOnlyList<TOut> SelectToList<TIn, TOut>(
        IEnumerable<TIn> items,
        Func<TIn, int, CancellationToken, TOut> map,
        LoopOptions? options,
        CancellationToken ct)
    {
        options ??= new LoopOptions();
        var list = new List<TOut>();
        var r = ForEach(items, (item, i, token) =>
        {
            list.Add(map(item, i, token));
            return LoopAction.Continue;
        }, options, ct);

        // If iteration was cancelled/bounded, we still return what was produced.
        _ = r;
        return list;
    }
}
