using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Security
{
    /// <summary>
    /// Interface for secure API key management.
    /// Part of Phase 3.3 security and compliance features.
    /// </summary>
    public interface ISecureApiKeyManager
    {
        /// <summary>
        /// Generates a new secure API key.
        /// </summary>
        /// <param name="name">Name for the API key.</param>
        /// <param name="description">Description of the API key.</param>
        /// <param name="expiration">Optional expiration time.</param>
        /// <param name="permissions">Optional permissions for the key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Generated API key information.</returns>
        Task<ApiKeyInfo> GenerateApiKeyAsync(
            string name, 
            string description, 
            TimeSpan? expiration = null,
            IEnumerable<string>? permissions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates an API key and returns its information.
        /// </summary>
        /// <param name="apiKey">The API key to validate.</param>
        /// <param name="requiredPermission">Optional required permission.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result with key information.</returns>
        Task<ApiKeyValidationResult> ValidateApiKeyAsync(
            string apiKey, 
            string? requiredPermission = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes an API key.
        /// </summary>
        /// <param name="keyId">ID of the key to revoke.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the key was revoked successfully.</returns>
        Task<bool> RevokeApiKeyAsync(string keyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all API keys.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of API key information.</returns>
        Task<IEnumerable<ApiKeyInfo>> ListApiKeysAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets API key usage statistics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Usage statistics.</returns>
        Task<ApiKeyUsageStatistics> GetUsageStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rotates an API key (generates new key, invalidates old one).
        /// </summary>
        /// <param name="keyId">ID of the key to rotate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>New API key information.</returns>
        Task<ApiKeyInfo> RotateApiKeyAsync(string keyId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// API key information model.
    /// </summary>
    public class ApiKeyInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string KeyHash { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
        public bool IsActive { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
        public DateTimeOffset? LastUsedAt { get; set; }
        public int UsageCount { get; set; }
    }

    /// <summary>
    /// API key validation result.
    /// </summary>
    public class ApiKeyValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public ApiKeyInfo? KeyInfo { get; set; }
    }

    /// <summary>
    /// API key usage statistics.
    /// </summary>
    public class ApiKeyUsageStatistics
    {
        public int TotalKeys { get; set; }
        public int ActiveKeys { get; set; }
        public int ExpiredKeys { get; set; }
        public int RevokedKeys { get; set; }
        public int TotalUsageCount { get; set; }
        public ApiKeyInfo? LastUsedKey { get; set; }
    }
}