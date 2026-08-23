namespace Ashlar.API.Security;

/// <summary>Private license status.</summary>
public sealed class PrivateLicenseStatus
{
    /// <summary>State.</summary>
    public PrivateLicenseState State { get; init; }

    /// <summary>Customer id.</summary>
    public string? CustomerId { get; init; }

    /// <summary>Tenant id.</summary>
    public string? TenantId { get; init; }

    /// <summary>Expires at.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Seats.</summary>
    public int Seats { get; init; }

    /// <summary>Detail.</summary>
    public string? Detail { get; init; }

    /// <summary>True when mutating API requests must be rejected due to license state.</summary>
    public bool BlocksMutatingRequests =>
        State is PrivateLicenseState.Expired or PrivateLicenseState.Invalid;
}
