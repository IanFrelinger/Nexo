using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.CLI.Commands;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Infrastructure.Trust;

namespace Ashlar.Tests.CLI.Tests.Commands;

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
            await TestAuditAsync_InvalidSince_ReturnsOneWithoutQueryingLog();
            await TestAuditAsync_InvalidUntil_ReturnsOneWithoutQueryingLog();
            await TestAuditAsync_DurationSince_PassesFilterToLog();
            await TestAuditAsync_InvalidCount_ReturnsOneWithoutQueryingLog();
            await TestDashboardAsync_InvalidCount_ReturnsOneWithoutQueryingLog();
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
        var auditLog = new Ashlar.BackgroundAgents.Trust.DataDecisionAuditLog();
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

    private async Task TestAuditAsync_InvalidSince_ReturnsOneWithoutQueryingLog()
    {
        var mockLog = new Mock<IDataDecisionAuditLog>();
        var logger = new Mock<ILogger<TrustCommand>>();
        var command = new TrustCommand(mockLog.Object, null, null, logger.Object);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var exitCode = await command.AuditAsync(10, "xyz", null, null, false, false, false);
            AssertEqual(1, exitCode);
            AssertTrue(writer.ToString().Contains("Invalid --since", StringComparison.Ordinal),
                "An invalid --since must be refused legibly.");
            mockLog.Verify(
                x => x.GetRecent(It.IsAny<int>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<string?>()),
                Times.Never);
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestAuditAsync_InvalidUntil_ReturnsOneWithoutQueryingLog()
    {
        var mockLog = new Mock<IDataDecisionAuditLog>();
        var logger = new Mock<ILogger<TrustCommand>>();
        var command = new TrustCommand(mockLog.Object, null, null, logger.Object);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var exitCode = await command.AuditAsync(10, null, "xyz", null, false, false, false);
            AssertEqual(1, exitCode);
            AssertTrue(writer.ToString().Contains("Invalid --until", StringComparison.Ordinal),
                "An invalid --until must be refused legibly.");
            mockLog.Verify(
                x => x.GetRecent(It.IsAny<int>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<string?>()),
                Times.Never);
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestAuditAsync_DurationSince_PassesFilterToLog()
    {
        var mockLog = new Mock<IDataDecisionAuditLog>();
        mockLog.Setup(x => x.GetRecent(It.IsAny<int>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<string?>()))
            .Returns(Array.Empty<Ashlar.Core.Application.Trust.Models.DataDecisionAuditEntry>());
        var logger = new Mock<ILogger<TrustCommand>>();
        var command = new TrustCommand(mockLog.Object, null, null, logger.Object);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var exitCode = await command.AuditAsync(10, "1h", null, null, false, false, false);
            AssertEqual(0, exitCode);
            mockLog.Verify(
                x => x.GetRecent(10, It.Is<DateTimeOffset?>(s => s.HasValue), null, null),
                Times.Once);
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestAuditAsync_InvalidCount_ReturnsOneWithoutQueryingLog()
    {
        var mockLog = new Mock<IDataDecisionAuditLog>(MockBehavior.Strict);
        var logger = new Mock<ILogger<TrustCommand>>();
        var command = new TrustCommand(mockLog.Object, null, null, logger.Object);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            foreach (var count in new[] { 0, -1 })
            {
                writer.GetStringBuilder().Clear();
                var exitCode = await command.AuditAsync(count, null, null, null, true, false, false);
                AssertEqual(1, exitCode);
                AssertTrue(writer.ToString().Contains("Invalid --count", StringComparison.Ordinal),
                    "A non-positive audit --count must be refused legibly.");
            }
            mockLog.VerifyNoOtherCalls();
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestDashboardAsync_InvalidCount_ReturnsOneWithoutQueryingLog()
    {
        var mockLog = new Mock<IDataDecisionAuditLog>(MockBehavior.Strict);
        var logger = new Mock<ILogger<TrustCommand>>();
        var command = new TrustCommand(mockLog.Object, null, null, logger.Object);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            foreach (var count in new[] { 0, -1 })
            {
                writer.GetStringBuilder().Clear();
                var exitCode = await command.DashboardAsync(count, true);
                AssertEqual(1, exitCode);
                AssertTrue(writer.ToString().Contains("Invalid --count", StringComparison.Ordinal),
                    "A non-positive dashboard --count must be refused legibly.");
            }
            mockLog.VerifyNoOtherCalls();
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
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
