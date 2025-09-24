using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Security;
using Nexo.Infrastructure.Services.Security.Auditors;

namespace Nexo.Infrastructure.Services.Security
{
    /// <summary>
    /// Orchestrator for production security auditing that delegates to specialized auditors.
    /// </summary>
    public class ProductionSecurityAuditor : IProductionSecurityAuditor
    {
        private readonly ILogger<ProductionSecurityAuditor> _logger;
        private readonly ApiKeySecurityAuditor _apiKeyAuditor;
        private readonly AuthenticationAuditor _authenticationAuditor;
        private readonly AuthorizationAuditor _authorizationAuditor;
        private readonly DataProtectionAuditor _dataProtectionAuditor;
        private readonly NetworkSecurityAuditor _networkSecurityAuditor;
        private readonly ComplianceAuditor _complianceAuditor;

        public ProductionSecurityAuditor(ILogger<ProductionSecurityAuditor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiKeyAuditor = new ApiKeySecurityAuditor(logger);
            _authenticationAuditor = new AuthenticationAuditor(logger);
            _authorizationAuditor = new AuthorizationAuditor(logger);
            _dataProtectionAuditor = new DataProtectionAuditor(logger);
            _networkSecurityAuditor = new NetworkSecurityAuditor(logger);
            _complianceAuditor = new ComplianceAuditor(logger);
        }

        public async Task<SecurityAuditResult> RunSecurityAuditAsync(
            SecurityAuditOptions options,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting comprehensive security audit");

            var result = new SecurityAuditResult
            {
                StartTime = DateTimeOffset.UtcNow,
                Options = options
            };

            try
            {
                // API Key Security Audit
                if (options.AuditApiKeys)
                {
                    result.ApiKeyAudit = await _apiKeyAuditor.AuditApiKeySecurityAsync(cancellationToken);
                }

                // Authentication Audit
                if (options.AuditAuthentication)
                {
                    result.AuthenticationAudit = await _authenticationAuditor.AuditAuthenticationAsync(cancellationToken);
                }

                // Authorization Audit
                if (options.AuditAuthorization)
                {
                    result.AuthorizationAudit = await _authorizationAuditor.AuditAuthorizationAsync(cancellationToken);
                }

                // Data Protection Audit
                if (options.AuditDataProtection)
                {
                    result.DataProtectionAudit = await _dataProtectionAuditor.AuditDataProtectionAsync(cancellationToken);
                }

                // Network Security Audit
                if (options.AuditNetworkSecurity)
                {
                    result.NetworkSecurityAudit = await _networkSecurityAuditor.AuditNetworkSecurityAsync(cancellationToken);
                }

                // Compliance Audit
                if (options.AuditCompliance)
                {
                    result.ComplianceAudit = await _complianceAuditor.AuditComplianceAsync(cancellationToken);
                }

                result.EndTime = DateTimeOffset.UtcNow;
                result.Success = true;
                result.Message = "Security audit completed successfully";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during security audit");
                result.EndTime = DateTimeOffset.UtcNow;
                result.Success = false;
                result.Message = $"Security audit failed: {ex.Message}";
                return result;
            }
        }
    }
}
