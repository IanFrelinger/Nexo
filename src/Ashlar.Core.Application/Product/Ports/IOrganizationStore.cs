using Ashlar.Core.Application.Product.Models;

namespace Ashlar.Core.Application.Product.Ports;

/// <summary>Minimal org control plane (Product Fleet Phase 2.3).</summary>
public interface IOrganizationStore
{
    Organization CreateOrganization(string name, string? tenantId = null);

    Organization? GetOrganization(string orgId);

    OrganizationMember AddMember(string orgId, string userId, OrganizationRole role);

    IReadOnlyList<OrganizationMember> GetMembers(string orgId);

    OrganizationRole? GetMemberRole(string orgId, string userId);
}
