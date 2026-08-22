namespace Ashlar.Core.Application.Product.Models;

/// <summary>Membership record linking a user to an organization with a role.</summary>
public sealed class OrganizationMember
{
    /// <summary>Organization identifier.</summary>
    public required string OrgId { get; init; }

    /// <summary>User identifier within the organization.</summary>
    public required string UserId { get; init; }

    /// <summary>Member's role and permissions within the organization.</summary>
    public OrganizationRole Role { get; init; }

    /// <summary>UTC timestamp when the user joined the organization.</summary>
    public DateTimeOffset JoinedAt { get; init; }
}
