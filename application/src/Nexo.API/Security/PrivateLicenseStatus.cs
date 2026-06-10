namespace Nexo.API.Security;

public enum PrivateLicenseState
{
    NotConfigured,
    Valid,
    Expired,
    Invalid,
}

public sealed class PrivateLicenseStatus
{
    public PrivateLicenseState State { get; init; }

    public string? CustomerId { get; init; }

    public string? TenantId { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public int Seats { get; init; }

    public string? Detail { get; init; }

    public bool BlocksMutatingRequests =>
        State is PrivateLicenseState.Expired or PrivateLicenseState.Invalid;
}
