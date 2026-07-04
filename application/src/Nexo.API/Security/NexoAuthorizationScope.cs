namespace Nexo.API.Security;

/// <summary>Authorization scope applied to Nexo API routes.</summary>
public enum NexoAuthorizationScope
{
    /// <summary>Mutating API endpoints only (POST, PUT, PATCH, DELETE).</summary>
    MutatingApi = 0,

    /// <summary>All API endpoints.</summary>
    AllApi = 1
}
