using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Security.Interfaces;

/// <summary>
/// Provides comprehensive authentication services with support for multiple protocols
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user with JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    Task<AuthenticationResult> AuthenticateWithJwtAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user with OAuth2
    /// </summary>
    /// <param name="oauthRequest">OAuth2 authentication request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    Task<AuthenticationResult> AuthenticateWithOAuth2Async(OAuth2Request oauthRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user with SAML
    /// </summary>
    /// <param name="samlRequest">SAML authentication request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    Task<AuthenticationResult> AuthenticateWithSamlAsync(SamlRequest samlRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user with multi-factor authentication
    /// </summary>
    /// <param name="mfaRequest">Multi-factor authentication request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    Task<AuthenticationResult> AuthenticateWithMfaAsync(MfaRequest mfaRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a JWT token for a user
    /// </summary>
    /// <param name="user">User information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT token result</returns>
    Task<JwtTokenResult> GenerateJwtTokenAsync(UserInfo user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes a JWT token
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT token result</returns>
    Task<JwtTokenResult> RefreshJwtTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a JWT token
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token validation result</returns>
    Task<TokenValidationResult> ValidateJwtTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a JWT token
    /// </summary>
    /// <param name="token">JWT token to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token revocation result</returns>
    Task<TokenRevocationResult> RevokeJwtTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user information from claims
    /// </summary>
    /// <param name="claims">User claims</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
    Task<UserInfo> GetUserFromClaimsAsync(IEnumerable<Claim> claims, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes user password
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="currentPassword">Current password</param>
    /// <param name="newPassword">New password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password change result</returns>
    Task<PasswordChangeResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets user password
    /// </summary>
    /// <param name="resetRequest">Password reset request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password reset result</returns>
    Task<PasswordResetResult> ResetPasswordAsync(PasswordResetRequest resetRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables multi-factor authentication for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="mfaType">MFA type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>MFA setup result</returns>
    Task<MfaSetupResult> EnableMfaAsync(string userId, MfaType mfaType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables multi-factor authentication for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>MFA disable result</returns>
    Task<MfaDisableResult> DisableMfaAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets authentication configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication configuration</returns>
    Task<AuthenticationConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Authentication result
/// </summary>
public record AuthenticationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public UserInfo? User { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public List<string> Errors { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// User information
/// </summary>
public record UserInfo
{
    public string UserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsLocked { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public List<string> Roles { get; init; } = new();
    public List<string> Permissions { get; init; } = new();
    public Dictionary<string, object> Claims { get; init; } = new();
}

/// <summary>
/// OAuth2 authentication request
/// </summary>
public record OAuth2Request
{
    public string Provider { get; init; } = string.Empty;
    public string AuthorizationCode { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
    public string? State { get; init; }
    public Dictionary<string, object> AdditionalParameters { get; init; } = new();
}

/// <summary>
/// SAML authentication request
/// </summary>
public record SamlRequest
{
    public string SamlResponse { get; init; } = string.Empty;
    public string RelayState { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public Dictionary<string, object> Attributes { get; init; } = new();
}

/// <summary>
/// Multi-factor authentication request
/// </summary>
public record MfaRequest
{
    public string UserId { get; init; } = string.Empty;
    public MfaType MfaType { get; init; }
    public string Code { get; init; } = string.Empty;
    public string? SessionToken { get; init; }
    public Dictionary<string, object> AdditionalData { get; init; } = new();
}

/// <summary>
/// Multi-factor authentication type
/// </summary>
public enum MfaType
{
    TOTP,
    SMS,
    Email,
    HardwareToken,
    Biometric
}

/// <summary>
/// JWT token result
/// </summary>
public record JwtTokenResult
{
    public bool IsSuccessful { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object> Claims { get; init; } = new();
}

/// <summary>
/// Token validation result
/// </summary>
public record TokenValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public UserInfo? User { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public List<string> Claims { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Token revocation result
/// </summary>
public record TokenRevocationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime RevokedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Password change result
/// </summary>
public record PasswordChangeResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Password reset request
/// </summary>
public record PasswordResetRequest
{
    public string Email { get; init; } = string.Empty;
    public string ResetToken { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public Dictionary<string, object> AdditionalData { get; init; } = new();
}

/// <summary>
/// Password reset result
/// </summary>
public record PasswordResetResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime ResetAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// MFA setup result
/// </summary>
public record MfaSetupResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public MfaType MfaType { get; init; }
    public string? SetupCode { get; init; }
    public string? QrCodeUrl { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object> SetupData { get; init; } = new();
}

/// <summary>
/// MFA disable result
/// </summary>
public record MfaDisableResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime DisabledAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Authentication configuration
/// </summary>
public record AuthenticationConfiguration
{
    public JwtConfiguration Jwt { get; init; } = new();
    public OAuth2Configuration OAuth2 { get; init; } = new();
    public SamlConfiguration Saml { get; init; } = new();
    public MfaConfiguration Mfa { get; init; } = new();
    public PasswordConfiguration Password { get; init; } = new();
    public List<string> EnabledProviders { get; init; } = new();
}

/// <summary>
/// JWT configuration
/// </summary>
public record JwtConfiguration
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public TimeSpan AccessTokenLifetime { get; init; }
    public TimeSpan RefreshTokenLifetime { get; init; }
    public bool ValidateIssuer { get; init; }
    public bool ValidateAudience { get; init; }
    public bool ValidateLifetime { get; init; }
}

/// <summary>
/// OAuth2 configuration
/// </summary>
public record OAuth2Configuration
{
    public Dictionary<string, OAuth2ProviderConfig> Providers { get; init; } = new();
    public string DefaultProvider { get; init; } = string.Empty;
    public TimeSpan TokenLifetime { get; init; }
}

/// <summary>
/// OAuth2 provider configuration
/// </summary>
public record OAuth2ProviderConfig
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string AuthorizationEndpoint { get; init; } = string.Empty;
    public string TokenEndpoint { get; init; } = string.Empty;
    public string UserInfoEndpoint { get; init; } = string.Empty;
    public List<string> Scopes { get; init; } = new();
}

/// <summary>
/// SAML configuration
/// </summary>
public record SamlConfiguration
{
    public string EntityId { get; init; } = string.Empty;
    public string SingleSignOnServiceUrl { get; init; } = string.Empty;
    public string SingleLogoutServiceUrl { get; init; } = string.Empty;
    public string CertificatePath { get; init; } = string.Empty;
    public string PrivateKeyPath { get; init; } = string.Empty;
    public List<string> AllowedIssuers { get; init; } = new();
}

/// <summary>
/// MFA configuration
/// </summary>
public record MfaConfiguration
{
    public List<MfaType> EnabledTypes { get; init; } = new();
    public int CodeLength { get; init; }
    public TimeSpan CodeLifetime { get; init; }
    public int MaxAttempts { get; init; }
    public bool RequireMfa { get; init; }
}

/// <summary>
/// Password configuration
/// </summary>
public record PasswordConfiguration
{
    public int MinimumLength { get; init; }
    public bool RequireUppercase { get; init; }
    public bool RequireLowercase { get; init; }
    public bool RequireDigit { get; init; }
    public bool RequireSpecialCharacter { get; init; }
    public int MaxAge { get; init; }
    public bool PreventReuse { get; init; }
    public int ReuseCount { get; init; }
} 