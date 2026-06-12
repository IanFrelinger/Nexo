namespace Nexo.Core.Application.Product.Models;

public sealed class OrganizationMember
{
    public required string OrgId { get; init; }

    public required string UserId { get; init; }

    public OrganizationRole Role { get; init; }

    public DateTimeOffset JoinedAt { get; init; }
}
