using Ashlar.Core.Application.Common.Ports;

namespace Ashlar.Core.Application.Common.Services;

/// <summary>
/// Optional loop kernel that can run iterations in parallel when enabled via LoopOptions.
/// Preserves sequential semantics when parallelism is disabled.
/// </summary>
public sealed class ParallelLoopKernel : ILoopKernel
{
    private readonly ILoopKernel _fallback;

    /// <summary>Creates a parallel kernel that delegates to <paramref name="fallback"/> when parallelism is disabled.</summary>
    /// <param name="fallback">Sequential kernel used when parallel execution is not requested.</param>
    public ParallelLoopKernel(ILoopKernel fallback)
    {
        _fallback = fallback;
    }

    public LoopResult ForEach<T>(
        IEnumerable<T> items,
        Func<T, int, CancellationToken, LoopAction> body,
        LoopOptions? options,
        CancellationToken ct)
        => _fallback.ForEach(items, body, options, ct);

    public async ValueTask<LoopResult> ForEachAsync<T>(
        IEnumerable<T> items,
        Func<T, int, CancellationToken, ValueTask<LoopAction>> body,
        LoopOptions? options,
        CancellationToken ct)
    {
        options ??= new LoopOptions();
        if (!options.EnableParallel)
        {
            return await _fallback.ForEachAsync(items, body, options, ct);
        }

        var started = DateTimeOffset.UtcNow;
        var max = options.MaxIterations;
        var budget = options.TimeBudget;
        var dop = options.MaxDegreeOfParallelism.GetValueOrDefault(Environment.ProcessorCount);
        if (dop < 1) dop = 1;

        var sem = new SemaphoreSlim(dop);
        var tasks = new List<Task>();
        var i = 0;

        foreach (var item in items)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }
            if (max.HasValue && i >= max.Value)
            {
                break;
            }
            if (budget.HasValue && DateTimeOffset.UtcNow - started > budget.Value)
            {
                break;
            }

            var localIndex = i;
            i++;

            await sem.WaitAsync(ct);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // In parallel mode, Break is not supported (ordering/short-circuiting is undefined).
                    _ = await body(item, localIndex, ct);
                }
                finally
                {
                    sem.Release();
                }
            }, ct));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // propagate below by marking not completed; caller sees exception already if awaited here
            throw;
        }

        var cancelled = ct.IsCancellationRequested;
        var timeExceeded = budget.HasValue && DateTimeOffset.UtcNow - started > budget.Value;
        var completed = !cancelled && !(max.HasValue && i >= max.Value) && !timeExceeded;

        return new LoopResult
        {
            Completed = completed,
            Cancelled = cancelled,
            TimeBudgetExceeded = timeExceeded,
            Iterations = i
        };
    }

    public IReadOnlyList<TOut> SelectToList<TIn, TOut>(
        IEnumerable<TIn> items,
        Func<TIn, int, CancellationToken, TOut> map,
        LoopOptions? options,
        CancellationToken ct)
        => _fallback.SelectToList(items, map, options, ct);
}

