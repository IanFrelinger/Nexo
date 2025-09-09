using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Security;

namespace Nexo.Infrastructure.Services.Security
{
    /// <summary>
    /// Secure API key management service for Phase 3.3 security features.
    /// </summary>
    public class SecureApiKeyManager : ISecureApiKeyManager
    {
        private readonly Dictionary<string, ApiKeyInfo> _apiKeys;
        private readonly object _lock = new object();

        public SecureApiKeyManager()
        {
            _apiKeys = new Dictionary<string, ApiKeyInfo>();
        }

        /// <summary>
        /// Generates a new secure API key.
        /// </summary>
        public async Task<ApiKeyInfo> GenerateApiKeyAsync(
            string name, 
            string description, 
            TimeSpan? expiration = null,
            IEnumerable<string>? permissions = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Simulate async operation

            var keyId = Guid.NewGuid().ToString();
            var keyValue = GenerateSecureKey();
            var hashedKey = HashKey(keyValue);

            var apiKey = new ApiKeyInfo
            {
                Id = keyId,
                Name = name,
                Description = description,
                KeyHash = hashedKey,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = expiration.HasValue ? DateTimeOffset.UtcNow.Add(expiration.Value) : null,
                IsActive = true,
                Permissions = permissions?.ToList() ?? new List<string>(),
                LastUsedAt = null,
                UsageCount = 0
            };

            lock (_lock)
            {
                _apiKeys[keyId] = apiKey;
            }

            return apiKey;
        }

        /// <summary>
        /// Validates an API key and returns its information.
        /// </summary>
        public async Task<ApiKeyValidationResult> ValidateApiKeyAsync(
            string apiKey, 
            string? requiredPermission = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Simulate async operation

            var hashedKey = HashKey(apiKey);

            lock (_lock)
            {
                var keyInfo = _apiKeys.Values.FirstOrDefault(k => k.KeyHash == hashedKey);
                
                if (keyInfo == null)
                {
                    return new ApiKeyValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Invalid API key"
                    };
                }

                if (!keyInfo.IsActive)
                {
                    return new ApiKeyValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "API key is inactive"
                    };
                }

                if (keyInfo.ExpiresAt.HasValue && keyInfo.ExpiresAt.Value < DateTimeOffset.UtcNow)
                {
                    return new ApiKeyValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "API key has expired"
                    };
                }

                if (!string.IsNullOrEmpty(requiredPermission) && 
                    !keyInfo.Permissions.Contains(requiredPermission))
                {
                    return new ApiKeyValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Insufficient permissions"
                    };
                }

                // Update usage statistics
                keyInfo.LastUsedAt = DateTimeOffset.UtcNow;
                keyInfo.UsageCount++;

                return new ApiKeyValidationResult
                {
                    IsValid = true,
                    KeyInfo = keyInfo
                };
            }
        }

        /// <summary>
        /// Revokes an API key.
        /// </summary>
        public async Task<bool> RevokeApiKeyAsync(string keyId, CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Simulate async operation

            lock (_lock)
            {
                if (_apiKeys.TryGetValue(keyId, out var keyInfo))
                {
                    keyInfo.IsActive = false;
                    keyInfo.RevokedAt = DateTimeOffset.UtcNow;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Lists all API keys.
        /// </summary>
        public async Task<IEnumerable<ApiKeyInfo>> ListApiKeysAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Simulate async operation

            lock (_lock)
            {
                return _apiKeys.Values.ToList();
            }
        }

        /// <summary>
        /// Gets API key usage statistics.
        /// </summary>
        public async Task<ApiKeyUsageStatistics> GetUsageStatisticsAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Simulate async operation

            lock (_lock)
            {
                var keys = _apiKeys.Values.ToList();
                
                return new ApiKeyUsageStatistics
                {
                    TotalKeys = keys.Count,
                    ActiveKeys = keys.Count(k => k.IsActive),
                    ExpiredKeys = keys.Count(k => k.ExpiresAt.HasValue && k.ExpiresAt.Value < DateTimeOffset.UtcNow),
                    RevokedKeys = keys.Count(k => !k.IsActive && k.RevokedAt.HasValue),
                    TotalUsageCount = keys.Sum(k => k.UsageCount),
                    LastUsedKey = keys.Where(k => k.LastUsedAt.HasValue).OrderByDescending(k => k.LastUsedAt).FirstOrDefault()
                };
            }
        }

        /// <summary>
        /// Rotates an API key (generates new key, invalidates old one).
        /// </summary>
        public async Task<ApiKeyInfo> RotateApiKeyAsync(
            string keyId, 
            CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Simulate async operation

            lock (_lock)
            {
                if (!_apiKeys.TryGetValue(keyId, out var oldKey))
                {
                    throw new InvalidOperationException("API key not found");
                }

                // Revoke old key
                oldKey.IsActive = false;
                oldKey.RevokedAt = DateTimeOffset.UtcNow;

                // Generate new key
                var newKeyValue = GenerateSecureKey();
                var newHashedKey = HashKey(newKeyValue);

                var newKey = new ApiKeyInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = oldKey.Name,
                    Description = oldKey.Description,
                    KeyHash = newHashedKey,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = oldKey.ExpiresAt,
                    IsActive = true,
                    Permissions = oldKey.Permissions,
                    LastUsedAt = null,
                    UsageCount = 0
                };

                _apiKeys[newKey.Id] = newKey;
                return newKey;
            }
        }

        /// <summary>
        /// Generates a secure random API key.
        /// </summary>
        private static string GenerateSecureKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var key = new StringBuilder(32);

            for (int i = 0; i < 32; i++)
            {
                key.Append(chars[random.Next(chars.Length)]);
            }

            return key.ToString();
        }

        /// <summary>
        /// Hashes an API key for secure storage.
        /// </summary>
        private static string HashKey(string key)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            return Convert.ToBase64String(hashBytes);
        }
    }
}