using Nexo.Core.Application.Product.Models;
using Nexo.Core.Application.Product.Ports;

namespace Nexo.Infrastructure.Product;

/// <summary>In-memory org registry for Cloud staging; replace with durable store for production.</summary>
public sealed class InMemoryOrganizationStore : IOrganizationStore
{
    private readonly object _lock = new();
    private readonly Dictionary<string, Organization> _orgs = new(StringComparer.Ordinal);
    private readonly List<OrganizationMember> _members = [];

    /// <summary>Create organization.</summary>
    public Organization CreateOrganization(string name, string? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name is required.", nameof(name));

        var orgId = Guid.NewGuid().ToString("D");
        var tid = string.IsNullOrWhiteSpace(tenantId)
            ? $"org-{orgId[..8]}"
            : tenantId.Trim();
        var org = new Organization
        {
            OrgId = orgId,
            Name = name.Trim(),
            TenantId = tid,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        lock (_lock)
        {
            _orgs[orgId] = org;
        }

        return org;
    }

    /// <summary>Gets organization.</summary>
    public Organization? GetOrganization(string orgId)
    {
        if (string.IsNullOrWhiteSpace(orgId))
            return null;

        lock (_lock)
        {
            return _orgs.TryGetValue(orgId.Trim(), out var org) ? org : null;
        }
    }

    /// <summary>Adds member.</summary>
    public OrganizationMember AddMember(string orgId, string userId, OrganizationRole role)
    {
        if (GetOrganization(orgId) is null)
            throw new InvalidOperationException($"Organization '{orgId}' was not found.");

        var normalizedUser = NormalizeUser(userId);
        var member = new OrganizationMember
        {
            OrgId = orgId.Trim(),
            UserId = normalizedUser,
            Role = role,
            JoinedAt = DateTimeOffset.UtcNow,
        };

        lock (_lock)
        {
            _members.RemoveAll(m =>
                string.Equals(m.OrgId, member.OrgId, StringComparison.Ordinal) &&
                string.Equals(m.UserId, member.UserId, StringComparison.Ordinal));
            _members.Add(member);
        }

        return member;
    }

    /// <summary>Gets members.</summary>
    public IReadOnlyList<OrganizationMember> GetMembers(string orgId)
    {
        if (string.IsNullOrWhiteSpace(orgId))
            return [];

        lock (_lock)
        {
            return _members
                .Where(m => string.Equals(m.OrgId, orgId.Trim(), StringComparison.Ordinal))
                .OrderBy(m => m.JoinedAt)
                .ToList();
        }
    }

    /// <summary>Gets member role.</summary>
    public OrganizationRole? GetMemberRole(string orgId, string userId)
    {
        if (string.IsNullOrWhiteSpace(orgId) || string.IsNullOrWhiteSpace(userId))
            return null;

        lock (_lock)
        {
            var member = _members.FirstOrDefault(m =>
                string.Equals(m.OrgId, orgId.Trim(), StringComparison.Ordinal) &&
                string.Equals(m.UserId, NormalizeUser(userId), StringComparison.Ordinal));
            return member?.Role;
        }
    }

    private static string NormalizeUser(string userId)
    {
        var trimmed = userId.Trim();
        if (trimmed.Length > 256)
            throw new ArgumentException("User id exceeds maximum length (256).", nameof(userId));
        return trimmed;
    }
}
