namespace Nexo.API.Security;

/// <summary>Supported Nexo API authorization credential modes.</summary>
public enum NexoAuthorizationMode
{
    /// <summary>No credential required.</summary>
    None = 0,

    /// <summary>API key only.</summary>
    ApiKey = 1,

    /// <summary>Bearer token only.</summary>
    BearerToken = 2,

    /// <summary>HTTP basic credentials only.</summary>
    Basic = 3,

    /// <summary>API key or bearer token.</summary>
    ApiKeyOrBearerToken = 4,

    /// <summary>API key or basic credentials.</summary>
    ApiKeyOrBasic = 5,

    /// <summary>Bearer token or basic credentials.</summary>
    BearerTokenOrBasic = 6,

    /// <summary>Any supported credential mode.</summary>
    Any = 7
}
