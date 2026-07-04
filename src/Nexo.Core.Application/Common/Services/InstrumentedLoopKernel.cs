using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;

namespace Nexo.Core.Application.Common.Services;

/// <summary>
/// Decorator that adds debug/trace logging around <see cref="ILoopKernel"/> operations.
/// Enabled when <c>NEXO_LOOP_INSTRUMENT=1</c> at host registration time.
/// </summary>
public sealed class InstrumentedLoopKernel : ILoopKernel
{
    private readonly ILoopKernel _inner;
    private readonly ILogger<InstrumentedLoopKernel> _logger;

    /// <summary>Wraps an inner loop kernel with structured logging.</summary>
    public InstrumentedLoopKernel(ILoopKernel inner, ILogger<InstrumentedLoopKernel> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    /// <inheritdoc />
    public LoopResult ForEach<T>(
        IEnumerable<T> items,
        Func<T, int, CancellationToken, LoopAction> body,
        LoopOptions? options,
        CancellationToken ct)
    {
        options ??= new LoopOptions();
        var name = options.Name ?? "loop";
        _logger.LogDebug("Loop start: {Name}", name);

        var r = _inner.ForEach(items, (item, i, token) =>
        {
            _logger.LogTrace("Loop iter: {Name} i={Index}", name, i);
            return body(item, i, token);
        }, options, ct);

        _logger.LogDebug("Loop end: {Name} completed={Completed} cancelled={Cancelled} iters={Iterations}",
            name, r.Completed, r.Cancelled, r.Iterations);

        return r;
    }

    /// <inheritdoc />
    public async ValueTask<LoopResult> ForEachAsync<T>(
        IEnumerable<T> items,
        Func<T, int, CancellationToken, ValueTask<LoopAction>> body,
        LoopOptions? options,
        CancellationToken ct)
    {
        options ??= new LoopOptions();
        var name = options.Name ?? "loop";
        _logger.LogDebug("Loop start: {Name}", name);

        var r = await _inner.ForEachAsync(items, async (item, i, token) =>
        {
            _logger.LogTrace("Loop iter: {Name} i={Index}", name, i);
            return await body(item, i, token);
        }, options, ct);

        _logger.LogDebug("Loop end: {Name} completed={Completed} cancelled={Cancelled} iters={Iterations}",
            name, r.Completed, r.Cancelled, r.Iterations);

        return r;
    }

    /// <inheritdoc />
    public IReadOnlyList<TOut> SelectToList<TIn, TOut>(
        IEnumerable<TIn> items,
        Func<TIn, int, CancellationToken, TOut> map,
        LoopOptions? options,
        CancellationToken ct)
    {
        options ??= new LoopOptions();
        var name = options.Name ?? "select";
        _logger.LogDebug("Select start: {Name}", name);
        var r = _inner.SelectToList(items, map, options, ct);
        _logger.LogDebug("Select end: {Name} count={Count}", name, r.Count);
        return r;
    }
}
