using System.Text.Json.Serialization;

namespace Nexo.API.Security;

/// <summary>On-disk Private license payload (JSON).</summary>
public sealed class PrivateLicenseDocument
{
    /// <summary>Licensed customer identifier.</summary>
    public string CustomerId { get; set; } = "";

    /// <summary>Tenant identifier bound to the license.</summary>
    public string TenantId { get; set; } = "";

    /// <summary>License expiration timestamp (UTC).</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Licensed seat count.</summary>
    public int Seats { get; set; }

    /// <summary>Base64 HMAC-SHA256 over canonical JSON without this field.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Signature { get; set; }
}
