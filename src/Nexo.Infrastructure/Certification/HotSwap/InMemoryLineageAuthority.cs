using System.Collections.Concurrent;
using Nexo.Core.Application.Autonomy;

namespace Nexo.Infrastructure.Certification.HotSwap;

/// <summary>Process-local lineage authority (R5.5). Demotion accumulates; there is no promotion.</summary>
public sealed class InMemoryLineageAuthority : ILineageAuthority
{
    private readonly ConcurrentDictionary<string, int> _rollbacks = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _threshold;

    /// <summary>Creates the authority with the spec-default demotion threshold.</summary>
    public InMemoryLineageAuthority(int threshold = LineageDemotion.DefaultThreshold) =>
        _threshold = Math.Max(1, threshold);

    /// <inheritdoc />
    public int RecordRollback(string lineageKey) =>
        string.IsNullOrWhiteSpace(lineageKey)
            ? 0
            : _rollbacks.AddOrUpdate(lineageKey, 1, (_, count) => count + 1);

    /// <inheritdoc />
    public bool IsDemoted(string lineageKey) =>
        !string.IsNullOrWhiteSpace(lineageKey)
        && _rollbacks.TryGetValue(lineageKey, out var count)
        && count >= _threshold;
}
