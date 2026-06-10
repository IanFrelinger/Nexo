using System.Text.Json.Serialization;

namespace Nexo.API.Security;

/// <summary>On-disk Private license payload (JSON).</summary>
public sealed class PrivateLicenseDocument
{
    public string CustomerId { get; set; } = "";

    public string TenantId { get; set; } = "";

    public DateTimeOffset ExpiresAt { get; set; }

    public int Seats { get; set; }

    /// <summary>Base64 HMAC-SHA256 over canonical JSON without this field.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Signature { get; set; }
}
