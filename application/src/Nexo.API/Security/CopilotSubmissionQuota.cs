using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Nexo.API.Security;

public interface ICopilotSubmissionQuota
{
    bool TryConsume(string tenantId, out string? denialMessage);
}

/// <summary>
/// Fixed-window hourly quota per tenant using in-memory counters (single-process).
/// </summary>
public sealed class CopilotSubmissionQuota : ICopilotSubmissionQuota
{
    private readonly IMemoryCache _cache;
    private readonly NexoEntitlementsOptions _options;

    public CopilotSubmissionQuota(IMemoryCache cache, IOptions<NexoEntitlementsOptions> optionsAccessor)
    {
        _cache = cache;
        _options = optionsAccessor.Value;
    }

    /// <inheritdoc />
    public bool TryConsume(string tenantId, out string? denialMessage)
    {
        denialMessage = null;
        var max = _options.MaxCopilotSubmissionsPerHour;
        if (max <= 0)
            return true;

        var tid = string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId.Trim();
        var bucket = DateTime.UtcNow.ToString("yyyyMMddHH", CultureInfo.InvariantCulture);
        var key = $"nexo:copilotquota:{tid}:{bucket}";
        var count = _cache.Get<int?>(key) ?? 0;
        if (count >= max)
        {
            denialMessage = $"Hourly copilot submission limit ({max}) reached for tenant '{tid}'.";
            return false;
        }

        _cache.Set(key, count + 1, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2) });
        return true;
    }
}
