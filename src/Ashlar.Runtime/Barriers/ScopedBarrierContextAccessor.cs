using System.Threading;
using Microsoft.Extensions.Options;
using Ashlar.Abstractions.Barriers;

namespace Ashlar.Runtime.Barriers;

/// <summary>
/// Scoped barrier context accessor that permits one-time initialization.
/// </summary>
public sealed class ScopedBarrierContextAccessor : IBarrierContextAccessor
{
    private readonly BarrierHierarchy _hierarchy;
    private readonly BarrierOptions _options;
    private readonly IBarrierContextAmbient? _ambient;
    private BarrierContext? _context;
    private int _initialized;

    public ScopedBarrierContextAccessor(
        BarrierHierarchy hierarchy,
        IOptions<BarrierOptions> options,
        IBarrierContextAmbient? ambient = null)
    {
        _hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _ambient = ambient;
    }

    /// <summary>Barrier context for the current scope, if initialized.</summary>
    public BarrierContext? Current => _context;

    /// <summary>Initializes the scoped barrier context; throws on ceiling violation or re-initialization.</summary>
    public void Initialize(BarrierContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

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
        _ambient?.SetCurrent(context);
    }
}
