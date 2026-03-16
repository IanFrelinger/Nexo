using System.Threading;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Barriers;

namespace Nexo.Runtime.Barriers;

/// <summary>
/// Scoped barrier context accessor that permits one-time initialization.
/// </summary>
public sealed class ScopedBarrierContextAccessor : IBarrierContextAccessor
{
    private readonly BarrierHierarchy _hierarchy;
    private readonly BarrierOptions _options;
    private BarrierContext? _context;
    private int _initialized;

    public ScopedBarrierContextAccessor(
        BarrierHierarchy hierarchy,
        IOptions<BarrierOptions> options)
    {
        _hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public BarrierContext? Current => _context;

    public void Initialize(BarrierContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!string.IsNullOrWhiteSpace(_options.HostCeiling) &&
            _hierarchy.IsAbove(context.Level, _options.HostCeiling!))
        {
            throw new BarrierCeilingExceededException(
                context.Level,
                _options.HostCeiling!,
                context.CorrelationId);
        }

        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
        {
            throw new InvalidOperationException(
                "BarrierContext has already been initialized for this scope. Re-initialization and elevation are not permitted.");
        }

        _context = context;
    }
}
