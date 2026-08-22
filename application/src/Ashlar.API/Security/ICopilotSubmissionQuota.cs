using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Ashlar.API.Security;

/// <summary>Hourly copilot submission quota enforcement per tenant.</summary>
public interface ICopilotSubmissionQuota
{
    /// <summary>Attempts to consume one submission slot for <paramref name="tenantId"/>; sets <paramref name="denialMessage"/> when denied.</summary>
    bool TryConsume(string tenantId, out string? denialMessage);
}
