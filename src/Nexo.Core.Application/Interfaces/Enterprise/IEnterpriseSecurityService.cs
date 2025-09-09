using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Models.Enterprise;

namespace Nexo.Core.Application.Interfaces.Enterprise
{
    /// <summary>
    /// Interface for enterprise security service in Phase 9.
    /// Provides comprehensive security features for enterprise integration.
    /// </summary>
    public interface IEnterpriseSecurityService
    {
        /// <summary>
        /// Creates enterprise security integration.
        /// </summary>
        /// <param name="securityConfig">The security configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Security integration result</returns>
        Task<SecurityIntegrationResult> CreateSecurityIntegrationAsync(
            SecurityConfiguration securityConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Implements compliance automation.
        /// </summary>
        /// <param name="complianceRequirements">The compliance requirements</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Compliance automation result</returns>
        Task<ComplianceAutomationResult> ImplementComplianceAutomationAsync(
            ComplianceRequirements complianceRequirements,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds enterprise governance features.
        /// </summary>
        /// <param name="governanceConfig">The governance configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Governance implementation result</returns>
        Task<GovernanceImplementationResult> AddEnterpriseGovernanceAsync(
            GovernanceConfiguration governanceConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates enterprise reporting system.
        /// </summary>
        /// <param name="reportingConfig">The reporting configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Reporting system result</returns>
        Task<ReportingSystemResult> CreateEnterpriseReportingAsync(
            ReportingConfiguration reportingConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates enterprise security compliance.
        /// </summary>
        /// <param name="validationConfig">The validation configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Security validation result</returns>
        Task<SecurityValidationResult> ValidateSecurityComplianceAsync(
            SecurityValidationConfiguration validationConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets enterprise security metrics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Security metrics</returns>
        Task<SecurityMetrics> GetSecurityMetricsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports enterprise security data.
        /// </summary>
        /// <param name="exportOptions">The export options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Security data export</returns>
        Task<SecurityDataExport> ExportSecurityDataAsync(
            SecurityExportOptions exportOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Imports enterprise security data.
        /// </summary>
        /// <param name="importData">The import data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Security data import result</returns>
        Task<SecurityDataImportResult> ImportSecurityDataAsync(
            SecurityImportData importData,
            CancellationToken cancellationToken = default);
    }
}
