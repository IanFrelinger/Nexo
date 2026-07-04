namespace Nexo.Core.Application.Product.Models;

/// <summary>Cloud control-plane organization (maps 1:1 to a copilot tenant scope).</summary>
public sealed class Organization
{
    /// <summary>Stable organization identifier.</summary>
    public required string OrgId { get; init; }

    /// <summary>Display name of the organization.</summary>
    public required string Name { get; init; }

    /// <summary>Stable tenant id used by copilot, usage, and audit APIs.</summary>
    public required string TenantId { get; init; }

    /// <summary>UTC timestamp when the organization was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
