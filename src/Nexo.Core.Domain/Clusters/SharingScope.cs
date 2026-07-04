namespace Nexo.Core.Domain.Clusters;

/// <summary>Visibility and sharing policy for a cluster definition.</summary>
public enum SharingScope
{
    /// <summary>Visible only to the creating user.</summary>
    Private,

    /// <summary>Shared within a team or organization.</summary>
    Team,

    /// <summary>Published to the community catalog.</summary>
    Community
}
