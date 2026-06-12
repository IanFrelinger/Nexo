namespace Nexo.Core.Application.Product.Models;

/// <summary>Cloud control-plane organization (maps 1:1 to a copilot tenant scope).</summary>
public sealed class Organization
{
    public required string OrgId { get; init; }

    public required string Name { get; init; }

    /// <summary>Stable tenant id used by copilot, usage, and audit APIs.</summary>
    public required string TenantId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
