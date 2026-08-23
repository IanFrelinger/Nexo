using Ashlar.Core.Application.Common.Ports;

namespace Ashlar.Core.Application.Common.Services;

/// <summary>
/// Loop kernel that can fan iterations out in parallel — but ONLY on the async path.
///
/// <para><see cref="ForEachAsync{T}"/> honours <c>LoopOptions.EnableParallel</c>. The two
/// synchronous overloads (<see cref="ForEach{T}"/>, <see cref="SelectToList{T,TResult}"/>)
/// always run sequentially by delegating to the fallback, regardless of the flag. This is by
/// design — there is no ambient async context to parallelise a synchronous body safely — and
/// is contract-compliant with the "may run in parallel" wording on <c>EnableParallel</c>.
/// Do not expect the flag to have any effect on the sync overloads.</para>
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

    /// <summary>
    /// Runs sequentially. <c>EnableParallel</c> is IGNORED on this synchronous overload — only
    /// <see cref="ForEachAsync{T}"/> fans out. Delegates to the fallback kernel.
    /// </summary>
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

