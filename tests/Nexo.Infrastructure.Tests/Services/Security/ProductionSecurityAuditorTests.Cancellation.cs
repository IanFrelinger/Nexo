using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Interfaces.Security;
using Nexo.Core.Application.Interfaces.Performance;
using Nexo.Infrastructure.Services.Security;

namespace Nexo.Infrastructure.Tests.Services.Security
{
    /// <summary>
    /// Cancellation tests for ProductionSecurityAuditor
    /// </summary>
    public partial class ProductionSecurityAuditorTests
    {
        [Fact]
        public async Task RunSecurityAuditAsync_WithCancellation_ShouldHandleCancellation()
        {
            // Arrange
            var options = new SecurityAuditOptions
            {
                AuditApiKeys = true,
                AuditAuthentication = true,
                AuditAuthorization = true,
                AuditEncryption = true,
                AuditAuditLogging = true,
                AuditNetwork = true,
                AuditCompliance = true,
                AuditPerformance = true
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _auditor.RunSecurityAuditAsync(options, cts.Token));
        }

        [Fact]
        public async Task RunPenetrationTestAsync_WithCancellation_ShouldHandleCancellation()
        {
            // Arrange
            var options = new PenetrationTestOptions
            {
                TestName = "Cancellation Test",
                TestAuthenticationBypass = true
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _auditor.RunPenetrationTestAsync(options, cts.Token));
        }
    }
}
