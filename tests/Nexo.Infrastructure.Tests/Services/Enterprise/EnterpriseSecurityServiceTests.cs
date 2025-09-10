using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Models.Enterprise;
using Nexo.Infrastructure.Services.Enterprise;

namespace Nexo.Infrastructure.Tests.Services.Enterprise
{
    /// <summary>
    /// Comprehensive E2E tests for Enterprise Security Service in Phase 9.
    /// Tests all enterprise security capabilities including security integration,
    /// compliance automation, and governance features.
    /// </summary>
    public class EnterpriseSecurityServiceTests : IDisposable
    {
        private readonly Mock<ILogger<EnterpriseSecurityService>> _mockLogger;
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator;
        private readonly EnterpriseSecurityService _enterpriseSecurityService;

        public EnterpriseSecurityServiceTests()
        {
            _mockLogger = new Mock<ILogger<EnterpriseSecurityService>>();
            _mockModelOrchestrator = new Mock<IModelOrchestrator>();
            _enterpriseSecurityService = new EnterpriseSecurityService(_mockLogger.Object, _mockModelOrchestrator.Object);
        }

        [Fact]
        public async Task IntegrateSecurityAsync_ValidSecurityConfig_ReturnsSuccessResult()
        {
            // Arrange
            var securityConfig = new SecurityConfig
            {
                Id = "test-security-1",
                Name = "Enterprise Security Config",
                SecurityLevel = "High",
                EncryptionEnabled = true,
                AuthenticationRequired = true,
                AuthorizationRules = new List<string> { "Role-based access", "Resource-based permissions" },
                ComplianceStandards = new List<string> { "SOC2", "GDPR", "HIPAA" },
                AuditEnabled = true
            };

            var mockResponse = new ModelResponse
            {
                Content = "Security integration completed successfully. Security level: High, Encryption: AES-256, Authentication: Multi-factor, Compliance: SOC2, GDPR, HIPAA. Audit trail: Enabled.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _enterpriseSecurityService.IntegrateSecurityAsync(securityConfig);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully integrated enterprise security", result.Message);
            Assert.Equal(securityConfig.Id, result.ConfigId);
            Assert.Equal(securityConfig.SecurityLevel, result.SecurityLevel);
            Assert.True(result.EncryptionEnabled);
            Assert.True(result.AuthenticationEnabled);
            Assert.NotEmpty(result.ComplianceStandards);
            Assert.True(result.AuditEnabled);
            Assert.NotNull(result.Metrics);
            Assert.True(result.IntegratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task AutomateComplianceAsync_ValidComplianceRequest_ReturnsComplianceResult()
        {
            // Arrange
            var complianceRequest = new ComplianceRequest
            {
                Id = "test-compliance-1",
                Standard = "SOC2",
                Requirements = new List<string> { "Access control", "Data encryption", "Audit logging" },
                Scope = "Full system",
                Priority = "High",
                Deadline = DateTimeOffset.UtcNow.AddDays(30)
            };

            var mockResponse = new ModelResponse
            {
                Content = "Compliance automation completed. Standard: SOC2, Compliance score: 95%, Missing requirements: 2, Recommendations: Implement data retention policy, Add incident response plan. Next review: 30 days.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _enterpriseSecurityService.AutomateComplianceAsync(complianceRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully automated compliance", result.Message);
            Assert.Equal(complianceRequest.Id, result.RequestId);
            Assert.Equal(complianceRequest.Standard, result.Standard);
            Assert.True(result.ComplianceScore > 0);
            Assert.True(result.MissingRequirements >= 0);
            Assert.NotEmpty(result.Recommendations);
            Assert.NotNull(result.NextReview);
            Assert.NotNull(result.Metrics);
            Assert.True(result.CompletedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task EnforceGovernanceAsync_ValidGovernancePolicy_ReturnsGovernanceResult()
        {
            // Arrange
            var governancePolicy = new GovernancePolicy
            {
                Id = "test-governance-1",
                Name = "Data Governance Policy",
                Type = "Data Protection",
                Rules = new List<string> { "Data classification", "Access control", "Retention policy" },
                EnforcementLevel = "Strict",
                ApplicableRoles = new List<string> { "Admin", "Manager", "User" },
                EffectiveDate = DateTimeOffset.UtcNow
            };

            var mockResponse = new ModelResponse
            {
                Content = "Governance enforcement completed. Policy: Data Governance Policy, Enforcement level: Strict, Applied to: 3 roles, Violations detected: 0, Compliance rate: 100%.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _enterpriseSecurityService.EnforceGovernanceAsync(governancePolicy);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully enforced governance policy", result.Message);
            Assert.Equal(governancePolicy.Id, result.PolicyId);
            Assert.Equal(governancePolicy.Name, result.PolicyName);
            Assert.Equal(governancePolicy.EnforcementLevel, result.EnforcementLevel);
            Assert.True(result.AppliedToRoles > 0);
            Assert.True(result.ViolationsDetected >= 0);
            Assert.True(result.ComplianceRate > 0);
            Assert.NotNull(result.Metrics);
            Assert.True(result.EnforcedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task AuditSecurityAsync_ValidAuditRequest_ReturnsAuditResult()
        {
            // Arrange
            var auditRequest = new SecurityAuditRequest
            {
                Id = "test-audit-1",
                AuditType = "Security Assessment",
                Scope = "Full system",
                FocusAreas = new List<string> { "Access control", "Data encryption", "Network security" },
                StartDate = DateTimeOffset.UtcNow.AddDays(-30),
                EndDate = DateTimeOffset.UtcNow
            };

            var mockResponse = new ModelResponse
            {
                Content = "Security audit completed. Type: Security Assessment, Scope: Full system, Findings: 5, Critical: 1, High: 2, Medium: 2, Low: 0. Recommendations: 8, Risk score: 7.5/10.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _enterpriseSecurityService.AuditSecurityAsync(auditRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully completed security audit", result.Message);
            Assert.Equal(auditRequest.Id, result.AuditId);
            Assert.Equal(auditRequest.AuditType, result.AuditType);
            Assert.Equal(auditRequest.Scope, result.Scope);
            Assert.True(result.TotalFindings > 0);
            Assert.True(result.CriticalFindings >= 0);
            Assert.True(result.HighFindings >= 0);
            Assert.True(result.MediumFindings >= 0);
            Assert.True(result.LowFindings >= 0);
            Assert.NotEmpty(result.Recommendations);
            Assert.True(result.RiskScore > 0);
            Assert.NotNull(result.Metrics);
            Assert.True(result.CompletedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GenerateSecurityReportAsync_ValidReportRequest_ReturnsSecurityReport()
        {
            // Arrange
            var reportRequest = new SecurityReportRequest
            {
                Id = "test-report-1",
                ReportType = "Compliance Report",
                Period = "Monthly",
                IncludeMetrics = true,
                IncludeRecommendations = true,
                Format = "PDF"
            };

            var mockResponse = new ModelResponse
            {
                Content = "Security report generated successfully. Type: Compliance Report, Period: Monthly, Format: PDF, Pages: 25, Sections: 8, Metrics: 15, Recommendations: 12.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _enterpriseSecurityService.GenerateSecurityReportAsync(reportRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully generated security report", result.Message);
            Assert.Equal(reportRequest.Id, result.RequestId);
            Assert.Equal(reportRequest.ReportType, result.ReportType);
            Assert.Equal(reportRequest.Period, result.Period);
            Assert.Equal(reportRequest.Format, result.Format);
            Assert.True(result.Pages > 0);
            Assert.True(result.Sections > 0);
            Assert.True(result.MetricsCount > 0);
            Assert.True(result.RecommendationsCount > 0);
            Assert.NotNull(result.ReportData);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task MonitorSecurityThreatsAsync_ValidMonitoringRequest_ReturnsThreatMonitoringResult()
        {
            // Arrange
            var monitoringRequest = new ThreatMonitoringRequest
            {
                Id = "test-monitoring-1",
                MonitoringType = "Real-time",
                ThreatTypes = new List<string> { "Malware", "Phishing", "DDoS" },
                Sensitivity = "High",
                AlertThreshold = 0.8
            };

            var mockResponse = new ModelResponse
            {
                Content = "Security threat monitoring activated. Type: Real-time, Sensitivity: High, Threshold: 0.8, Threats detected: 3, Alerts generated: 2, Status: Active.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _enterpriseSecurityService.MonitorSecurityThreatsAsync(monitoringRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully activated security threat monitoring", result.Message);
            Assert.Equal(monitoringRequest.Id, result.RequestId);
            Assert.Equal(monitoringRequest.MonitoringType, result.MonitoringType);
            Assert.Equal(monitoringRequest.Sensitivity, result.Sensitivity);
            Assert.Equal(monitoringRequest.AlertThreshold, result.AlertThreshold);
            Assert.True(result.ThreatsDetected >= 0);
            Assert.True(result.AlertsGenerated >= 0);
            Assert.NotNull(result.Status);
            Assert.NotNull(result.Metrics);
            Assert.True(result.ActivatedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task IntegrateSecurityAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var securityConfig = new SecurityConfig
            {
                Id = "test-security-error",
                Name = "Error Security Config"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _enterpriseSecurityService.IntegrateSecurityAsync(securityConfig);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(securityConfig.Id, result.ConfigId);
            Assert.True(result.IntegratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task AutomateComplianceAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var complianceRequest = new ComplianceRequest
            {
                Id = "test-compliance-error",
                Standard = "SOC2"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _enterpriseSecurityService.AutomateComplianceAsync(complianceRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(complianceRequest.Id, result.RequestId);
            Assert.True(result.CompletedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task EnforceGovernanceAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var governancePolicy = new GovernancePolicy
            {
                Id = "test-governance-error",
                Name = "Error Policy"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _enterpriseSecurityService.EnforceGovernanceAsync(governancePolicy);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(governancePolicy.Id, result.PolicyId);
            Assert.True(result.EnforcedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task AuditSecurityAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var auditRequest = new SecurityAuditRequest
            {
                Id = "test-audit-error",
                AuditType = "Security Assessment"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _enterpriseSecurityService.AuditSecurityAsync(auditRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(auditRequest.Id, result.AuditId);
            Assert.True(result.CompletedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GenerateSecurityReportAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var reportRequest = new SecurityReportRequest
            {
                Id = "test-report-error",
                ReportType = "Compliance Report"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _enterpriseSecurityService.GenerateSecurityReportAsync(reportRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(reportRequest.Id, result.RequestId);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task MonitorSecurityThreatsAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var monitoringRequest = new ThreatMonitoringRequest
            {
                Id = "test-monitoring-error",
                MonitoringType = "Real-time"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _enterpriseSecurityService.MonitorSecurityThreatsAsync(monitoringRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(monitoringRequest.Id, result.RequestId);
            Assert.True(result.ActivatedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task IntegrateSecurityAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var securityConfig = new SecurityConfig
            {
                Id = "test-security-cancel",
                Name = "Cancel Security Config"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _enterpriseSecurityService.IntegrateSecurityAsync(securityConfig, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task AutomateComplianceAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var complianceRequest = new ComplianceRequest
            {
                Id = "test-compliance-cancel",
                Standard = "SOC2"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _enterpriseSecurityService.AutomateComplianceAsync(complianceRequest, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task EnforceGovernanceAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var governancePolicy = new GovernancePolicy
            {
                Id = "test-governance-cancel",
                Name = "Cancel Policy"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _enterpriseSecurityService.EnforceGovernanceAsync(governancePolicy, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task AuditSecurityAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var auditRequest = new SecurityAuditRequest
            {
                Id = "test-audit-cancel",
                AuditType = "Security Assessment"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _enterpriseSecurityService.AuditSecurityAsync(auditRequest, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GenerateSecurityReportAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var reportRequest = new SecurityReportRequest
            {
                Id = "test-report-cancel",
                ReportType = "Compliance Report"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _enterpriseSecurityService.GenerateSecurityReportAsync(reportRequest, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task MonitorSecurityThreatsAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var monitoringRequest = new ThreatMonitoringRequest
            {
                Id = "test-monitoring-cancel",
                MonitoringType = "Real-time"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _enterpriseSecurityService.MonitorSecurityThreatsAsync(monitoringRequest, cancellationTokenSource.Token));
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
