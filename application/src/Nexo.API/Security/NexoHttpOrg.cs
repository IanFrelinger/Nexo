using Nexo.Core.Application.Product.Models;
using Nexo.Core.Application.Product.Ports;

namespace Nexo.API.Security;

/// <summary>
/// Resolves cloud org membership and tenant scope from staging headers (Phase 2.3).
/// </summary>
public static class NexoHttpOrg
{
    public const string UserHeaderName = "X-Nexo-User";
    public const string OrgHeaderName = "X-Nexo-Org";

    public static bool TryResolveUser(HttpRequest request, NexoProductOptions options, out string userId, out string? errorDetail)
    {
        userId = "";
        errorDetail = null;
        var header = string.IsNullOrWhiteSpace(options.UserHeaderName)
            ? UserHeaderName
            : options.UserHeaderName.Trim();

        var raw = request.Headers[header].ToString().Trim();
        if (string.IsNullOrEmpty(raw))
        {
            errorDetail = $"User header '{header}' is required when org membership is enforced.";
            return false;
        }

        if (raw.Length > 256)
        {
            errorDetail = $"User header '{header}' exceeds maximum length (256).";
            return false;
        }

        userId = raw;
        return true;
    }

    public static bool TryResolveOrgContext(
        HttpRequest request,
        NexoProductOptions options,
        IOrganizationStore orgStore,
        out Organization organization,
        out string userId,
        out OrganizationRole role,
        out string? errorDetail)
    {
        organization = null!;
        userId = "";
        role = OrganizationRole.Member;
        errorDetail = null;

        if (!TryResolveUser(request, options, out userId, out errorDetail))
            return false;

        var orgHeader = string.IsNullOrWhiteSpace(options.OrgHeaderName)
            ? OrgHeaderName
            : options.OrgHeaderName.Trim();
        var orgId = request.Headers[orgHeader].ToString().Trim();
        if (string.IsNullOrEmpty(orgId))
        {
            errorDetail = $"Org header '{orgHeader}' is required when org membership is enforced.";
            return false;
        }

        var org = orgStore.GetOrganization(orgId);
        if (org is null)
        {
            errorDetail = $"Organization '{orgId}' was not found.";
            return false;
        }

        var memberRole = orgStore.GetMemberRole(orgId, userId);
        if (memberRole is null)
        {
            errorDetail = $"User is not a member of organization '{orgId}'.";
            return false;
        }

        organization = org;
        role = memberRole.Value;
        return true;
    }

    public static bool TenantMatchesOrg(string tenantId, Organization organization) =>
        string.Equals(tenantId, organization.TenantId, StringComparison.Ordinal);
}
