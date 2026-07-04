using Microsoft.Extensions.Logging;
using Moq;
using Nexo.CLI.Commands;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure.Trust;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>Tests for trust command.</summary>
public class TrustCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test audit async_with audit log_returns zero.</summary>
            await TestAuditAsync_WithAuditLog_ReturnsZero();
            /// <summary>Test audit async_without audit log_returns one.</summary>
            await TestAuditAsync_WithoutAuditLog_ReturnsOne();
            /// <summary>Test pause async_with boundary_returns zero.</summary>
            await TestPauseAsync_WithBoundary_ReturnsZero();
            /// <summary>Test resume async_with boundary_returns zero.</summary>
            await TestResumeAsync_WithBoundary_ReturnsZero();
            /// <summary>Test allow async_with category_sets allowed.</summary>
            await TestAllowAsync_WithCategory_SetsAllowed();
            /// <summary>Test boundary async_with boundary_returns zero.</summary>
            await TestBoundaryAsync_WithBoundary_ReturnsZero();
            return new TestResult
            {
                Name = nameof(TrustCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All TrustCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(TrustCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(TrustCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestAuditAsync_WithAuditLog_ReturnsZero()
    {
        var auditLog = new Nexo.BackgroundAgents.Trust.DataDecisionAuditLog();
        var logger = new Mock<ILogger<TrustCommand>>();
        var command = new TrustCommand(auditLog, null, null, logger.Object);

        var exitCode = await command.AuditAsync(10, null, null, null, false, false, false);

        /// <summary>Assert equal.</summary>
        AssertEqual(0, exitCode);
    }

    private async Task TestAuditAsync_WithoutAuditLog_ReturnsOne()
    {
        var logger = new Mock<ILogger<TrustCommand>>();
        var command = new TrustCommand(null, null, null, logger.Object);

        var exitCode = await command.AuditAsync(10, null, null, null, false, false, false);

        /// <summary>Assert equal.</summary>
        AssertEqual(1, exitCode);
    }

    private async Task TestPauseAsync_WithBoundary_ReturnsZero()
    {
        var boundary = new AccessBoundary();
        var logger = new Mock<ILogger<TrustCommand>>();
        var command = new TrustCommand(null, boundary, null, logger.Object);

        var exitCode = await command.PauseAsync(false);

        /// <summary>Assert equal.</summary>
        AssertEqual(0, exitCode);
        /// <summary>Assert true.</summary>
        AssertTrue(boundary.IsObservationPaused);
    }

    private async Task TestResumeAsync_WithBoundary_ReturnsZero()
    {
        var boundary = new AccessBoundary();
        boundary.SetPause(true);
        var logger = new Mock<ILogger<TrustCommand>>();
        var command = new TrustCommand(null, boundary, null, logger.Object);

        var exitCode = await command.ResumeAsync(false);

        /// <summary>Assert equal.</summary>
        AssertEqual(0, exitCode);
        /// <summary>Assert false.</summary>
        AssertFalse(boundary.IsObservationPaused);
    }

    private async Task TestAllowAsync_WithCategory_SetsAllowed()
    {
        var mockBoundary = new Mock<IAccessBoundary>();
        mockBoundary.Setup(x => x.IsObservationPaused).Returns(false);
        var logger = new Mock<ILogger<TrustCommand>>();
        var command = new TrustCommand(null, mockBoundary.Object, null, logger.Object);

        var exitCode = await command.AllowAsync("file-paths", null, null, false);

        /// <summary>Assert equal.</summary>
        AssertEqual(0, exitCode);
        mockBoundary.Verify(x => x.SetCategoryAllowed("file-paths", true), Times.Once);
    }

    private async Task TestBoundaryAsync_WithBoundary_ReturnsZero()
    {
        var boundary = new AccessBoundary();
        var logger = new Mock<ILogger<TrustCommand>>();
        var command = new TrustCommand(null, boundary, null, logger.Object);

        var exitCode = await command.BoundaryAsync(false);

        /// <summary>Assert equal.</summary>
        AssertEqual(0, exitCode);
    }
}
