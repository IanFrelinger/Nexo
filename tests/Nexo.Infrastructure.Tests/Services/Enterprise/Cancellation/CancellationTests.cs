using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Models.Enterprise;
using Nexo.Infrastructure.Services.Enterprise;

namespace Nexo.Infrastructure.Tests.Services.Enterprise.Cancellation
{
    public class CancellationTests
    {
        private readonly Mock<ILogger<EnterpriseSecurityService>> _mockLogger;
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator;
        private readonly EnterpriseSecurityService _enterpriseSecurityService;

        public CancellationTests()
        {
            _mockLogger = new Mock<ILogger<EnterpriseSecurityService>>();
            _mockModelOrchestrator = new Mock<IModelOrchestrator>();
            _enterpriseSecurityService = new EnterpriseSecurityService(_mockLogger.Object, _mockModelOrchestrator.Object);
        }

        [Fact]
        public async Task IntegrateSecurityAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            var securityConfig = new SecurityConfig { Id = "test-security-cancel", Name = "Cancel Security Config" };
            var cts = new CancellationTokenSource();
            cts.Cancel();
            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _enterpriseSecurityService.IntegrateSecurityAsync(securityConfig, cts.Token));
        }

        [Fact]
        public async Task AutomateComplianceAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            var complianceRequest = new ComplianceRequest { Id = "test-compliance-cancel", Standard = "SOC2" };
            var cts = new CancellationTokenSource();
            cts.Cancel();
            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _enterpriseSecurityService.AutomateComplianceAsync(complianceRequest, cts.Token));
        }

        [Fact]
        public async Task EnforceGovernanceAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            var governancePolicy = new GovernancePolicy { Id = "test-governance-cancel", Name = "Cancel Policy" };
            var cts = new CancellationTokenSource();
            cts.Cancel();
            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _enterpriseSecurityService.EnforceGovernanceAsync(governancePolicy, cts.Token));
        }

        [Fact]
        public async Task AuditSecurityAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            var auditRequest = new SecurityAuditRequest { Id = "test-audit-cancel", AuditType = "Security Assessment" };
            var cts = new CancellationTokenSource();
            cts.Cancel();
            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _enterpriseSecurityService.AuditSecurityAsync(auditRequest, cts.Token));
        }

        [Fact]
        public async Task GenerateSecurityReportAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            var reportRequest = new SecurityReportRequest { Id = "test-report-cancel", ReportType = "Compliance Report" };
            var cts = new CancellationTokenSource();
            cts.Cancel();
            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _enterpriseSecurityService.GenerateSecurityReportAsync(reportRequest, cts.Token));
        }

        [Fact]
        public async Task MonitorSecurityThreatsAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            var monitoringRequest = new ThreatMonitoringRequest { Id = "test-monitoring-cancel", MonitoringType = "Real-time" };
            var cts = new CancellationTokenSource();
            cts.Cancel();
            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _enterpriseSecurityService.MonitorSecurityThreatsAsync(monitoringRequest, cts.Token));
        }
    }
}

