namespace Nexo.API.Security;

/// <summary>Contract for private license validator.</summary>
public interface IPrivateLicenseValidator
{
    /// <summary>Gets status.</summary>
    PrivateLicenseStatus GetStatus();
}
