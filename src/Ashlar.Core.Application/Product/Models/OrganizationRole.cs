namespace Ashlar.Core.Application.Product.Models;

/// <summary>Role assigned to an organization member.</summary>
public enum OrganizationRole
{
    /// <summary>Standard member with default permissions.</summary>
    Member = 0,

    /// <summary>Administrator with elevated organization management permissions.</summary>
    Admin = 1,
}
