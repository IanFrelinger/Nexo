using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Security;

namespace Nexo.Infrastructure.Services.Security
{
    /// <summary>
    /// API key management functionality for security compliance service.
    /// </summary>
    public partial class SecurityComplianceService
    {
        /// <summary>
        /// Validates API key and logs the access attempt.
        /// </summary>
        public async Task<ApiKeyValidationResult> ValidateApiKeyWithAuditAsync(
            string apiKey, 
            string? requiredPermission = null,
            string? userId = null,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken cancellationToken = default)
        {
            var validationResult = await _apiKeyManager.ValidateApiKeyAsync(apiKey, requiredPermission, cancellationToken);

            // Log the access attempt
            var auditEvent = new SecurityEvent
            {
                EventType = SecurityEventType.AuthenticationFailure,
                Description = validationResult.IsValid ? "API key validation successful" : $"API key validation failed: {validationResult.ErrorMessage}",
                Severity = validationResult.IsValid ? SecurityEventSeverity.Low : SecurityEventSeverity.Medium,
                UserId = userId ?? "unknown",
                Resource = "API",
                Action = "ValidateApiKey",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsBlocked = !validationResult.IsValid
            };

            await _auditLogger.LogSecurityEventAsync(auditEvent, cancellationToken);

            return validationResult;
        }

        /// <summary>
        /// Generates a new API key with audit logging.
        /// </summary>
        public async Task<ApiKeyInfo> GenerateApiKeyWithAuditAsync(
            string name, 
            string description, 
            string userId,
            TimeSpan? expiration = null,
            IEnumerable<string>? permissions = null,
            CancellationToken cancellationToken = default)
        {
            var apiKey = await _apiKeyManager.GenerateApiKeyAsync(name, description, expiration, permissions, cancellationToken);

            // Log the API key generation
            var auditEvent = new AuditEvent
            {
                EventType = AuditEventType.SystemConfiguration,
                Description = $"API key generated: {name}",
                Severity = AuditEventSeverity.Info,
                UserId = userId,
                Resource = "API Key Management",
                Action = "GenerateApiKey",
                Metadata = new Dictionary<string, object>
                {
                    ["KeyId"] = apiKey.Id,
                    ["KeyName"] = apiKey.Name,
                    ["Expiration"] = apiKey.ExpiresAt?.ToString() ?? "Never",
                    ["Permissions"] = string.Join(",", apiKey.Permissions)
                }
            };

            await _auditLogger.LogAuditEventAsync(auditEvent, cancellationToken);

            return apiKey;
        }

        /// <summary>
        /// Revokes an API key with audit logging.
        /// </summary>
        public async Task<bool> RevokeApiKeyWithAuditAsync(
            string keyId, 
            string userId,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            var success = await _apiKeyManager.RevokeApiKeyAsync(keyId, cancellationToken);

            if (success)
            {
                // Log the API key revocation
                var auditEvent = new AuditEvent
                {
                    EventType = AuditEventType.SystemConfiguration,
                    Description = $"API key revoked: {keyId}",
                    Severity = AuditEventSeverity.Warning,
                    UserId = userId,
                    Resource = "API Key Management",
                    Action = "RevokeApiKey",
                    Metadata = new Dictionary<string, object>
                    {
                        ["KeyId"] = keyId,
                        ["Reason"] = reason ?? "No reason provided"
                    }
                };

                await _auditLogger.LogAuditEventAsync(auditEvent, cancellationToken);
            }

            return success;
        }
    }
}
