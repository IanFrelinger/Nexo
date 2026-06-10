using Nexo.Core.Application.Product.Models;
using Nexo.Core.Application.Product.Ports;

namespace Nexo.Infrastructure.Product;

/// <summary>Thread-safe in-memory tenant usage counters (single-process; replace with durable store for Cloud).</summary>
public sealed class InMemoryTenantUsageStore : ITenantUsageStore
{
    private readonly object _lock = new();
    private readonly List<UsageEntry> _entries = [];

    public void RecordJobSubmitted(string tenantId)
        => Append(NormalizeTenant(tenantId), submitted: true, success: null);

    public void RecordJobCompleted(string tenantId, bool success)
        => Append(NormalizeTenant(tenantId), submitted: false, success: success);

    public TenantUsageSummary GetSummary(string tenantId, int windowHours)
    {
        var tid = NormalizeTenant(tenantId);
        var hours = Math.Clamp(windowHours, 1, 168);
        var since = DateTimeOffset.UtcNow.AddHours(-hours);

        lock (_lock)
        {
            PruneOlderThan(DateTimeOffset.UtcNow.AddDays(-7));
            var window = _entries.Where(e => e.TenantId == tid && e.At >= since).ToList();
            var submitted = window.Count(e => e.Submitted);
            var completed = window.Where(e => e.Success.HasValue).ToList();
            return new TenantUsageSummary(
                tid,
                hours,
                submitted,
                completed.Count(e => e.Success == true),
                completed.Count(e => e.Success == false));
        }
    }

    private void Append(string tenantId, bool submitted, bool? success)
    {
        lock (_lock)
        {
            _entries.Add(new UsageEntry(tenantId, DateTimeOffset.UtcNow, submitted, success));
            if (_entries.Count > 50_000)
                _entries.RemoveRange(0, _entries.Count - 40_000);
        }
    }

    private void PruneOlderThan(DateTimeOffset cutoff)
    {
        _entries.RemoveAll(e => e.At < cutoff);
    }

    private static string NormalizeTenant(string tenantId)
        => string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId.Trim();

    private sealed record UsageEntry(string TenantId, DateTimeOffset At, bool Submitted, bool? Success);
}
