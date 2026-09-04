namespace Ashlar.Cloud;

/// <summary>Hosted organization record. Not a kernel type.</summary>
/// <param name="OrganizationId">Stable org id.</param>
/// <param name="DisplayName">Human-readable name.</param>
public sealed record Organization(string OrganizationId, string DisplayName)
{
    /// <summary>Rejects blank identifiers on every construction path.</summary>
    public Organization
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(OrganizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(DisplayName);
        OrganizationId = OrganizationId.Trim();
        DisplayName = DisplayName.Trim();
    }
}

/// <summary>Quota ceiling for one organization.</summary>
/// <param name="OrganizationId">Owning org.</param>
/// <param name="MaxConcurrentTasks">Cluster concurrency cap.</param>
/// <param name="MaxMonthlyTokenBudget">Optional model-token budget. Null means unlimited.</param>
public sealed record OrganizationQuota(
    string OrganizationId,
    int MaxConcurrentTasks,
    long? MaxMonthlyTokenBudget)
{
    /// <summary>Rejects blank ids and non-positive ceilings on every construction path.</summary>
    public OrganizationQuota
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(OrganizationId);
        if (MaxConcurrentTasks < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxConcurrentTasks), MaxConcurrentTasks, "Must be at least 1.");
        }

        if (MaxMonthlyTokenBudget is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxMonthlyTokenBudget), MaxMonthlyTokenBudget, "Must be null or non-negative.");
        }

        OrganizationId = OrganizationId.Trim();
    }
}

/// <summary>Billing account attached to an organization.</summary>
/// <param name="OrganizationId">Owning org.</param>
/// <param name="PlanId">Commercial plan identifier.</param>
public sealed record BillingAccount(string OrganizationId, string PlanId)
{
    /// <summary>Rejects blank identifiers on every construction path.</summary>
    public BillingAccount
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(OrganizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(PlanId);
        OrganizationId = OrganizationId.Trim();
        PlanId = PlanId.Trim();
    }
}

/// <summary>Directory of hosted organizations and their quotas.</summary>
public interface IOrganizationDirectory
{
    /// <summary>Creates or replaces <paramref name="organization"/>.</summary>
    void Upsert(Organization organization, OrganizationQuota quota, BillingAccount billing);

    /// <summary>Returns the organization, or <see langword="null"/> if unknown.</summary>
    Organization? GetOrganization(string organizationId);

    /// <summary>Returns the quota, or <see langword="null"/> if unknown.</summary>
    OrganizationQuota? GetQuota(string organizationId);

    /// <summary>Returns the billing account, or <see langword="null"/> if unknown.</summary>
    BillingAccount? GetBilling(string organizationId);
}

/// <summary>In-memory control-plane directory for the extractable cloud scaffold.</summary>
public sealed class InMemoryOrganizationDirectory : IOrganizationDirectory
{
    private readonly Dictionary<string, (Organization Org, OrganizationQuota Quota, BillingAccount Billing)> _items =
        new(StringComparer.Ordinal);

    /// <inheritdoc />
    public void Upsert(Organization organization, OrganizationQuota quota, BillingAccount billing)
    {
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(quota);
        ArgumentNullException.ThrowIfNull(billing);
        ArgumentException.ThrowIfNullOrWhiteSpace(organization.OrganizationId);

        if (!string.Equals(organization.OrganizationId, quota.OrganizationId, StringComparison.Ordinal) ||
            !string.Equals(organization.OrganizationId, billing.OrganizationId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Organization, quota, and billing ids must match.", nameof(organization));
        }

        _items[organization.OrganizationId] = (organization, quota, billing);
    }

    /// <inheritdoc />
    public Organization? GetOrganization(string organizationId) =>
        TryGet(organizationId, out var row) ? row.Org : null;

    /// <inheritdoc />
    public OrganizationQuota? GetQuota(string organizationId) =>
        TryGet(organizationId, out var row) ? row.Quota : null;

    /// <inheritdoc />
    public BillingAccount? GetBilling(string organizationId) =>
        TryGet(organizationId, out var row) ? row.Billing : null;

    private bool TryGet(
        string organizationId,
        out (Organization Org, OrganizationQuota Quota, BillingAccount Billing) row)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        return _items.TryGetValue(organizationId, out row);
    }
}
