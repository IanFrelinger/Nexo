using Nexo.Agents.UniversalTester.Adapters;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Agents;

/// <summary>
/// Unit tests for DesktopAdapter: Wait action without connection, error collection,
/// and connection validation.
/// </summary>
public class DesktopAdapterTests : UnitTestBase
{
    [Fact]
    public async Task DesktopAdapter_AllTests_Pass()
    {
        var result = await ExecuteAsync(CancellationToken.None);
        Assert.True(result.Passed, result.ErrorMessage ?? result.Message);
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestExecuteAction_Wait_WithoutConnection_ReturnsWaited();
            await TestConnectAsync_WhenTargetNotFound_ThrowsAndRecordsError();
            await TestGetErrorsAsync_ReturnsCollectedErrors();
            await TestIsConnected_BeforeConnect_IsFalse();
            return new TestResult
            {
                Name = nameof(DesktopAdapterTests),
                Category = "Agents",
                Passed = true,
                Message = "All DesktopAdapter tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(DesktopAdapterTests),
                Category = "Agents",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(DesktopAdapterTests),
                Category = "Agents",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestExecuteAction_Wait_WithoutConnection_ReturnsWaited()
    {
        var adapter = new DesktopAdapter(null);
        var action = new TestAction
        {
            Id = "wait_1",
            Type = ActionType.Wait,
            Description = "Wait 100ms",
            Duration = TimeSpan.FromMilliseconds(100)
        };

        var result = await adapter.ExecuteActionAsync(action, CancellationToken.None);

        AssertNotNull(result);
        AssertEqual("Waited", result);
    }

    private async Task TestConnectAsync_WhenTargetNotFound_ThrowsAndRecordsError()
    {
        var adapter = new DesktopAdapter(null);
        var target = "process://NonExistentProcessName_" + Guid.NewGuid().ToString("N")[..8];

        await AssertThrowsAsync<InvalidOperationException>(async () =>
            await adapter.ConnectAsync(target, CancellationToken.None));

        var errors = await adapter.GetErrorsAsync(CancellationToken.None);
        AssertTrue(errors.Count > 0, "Errors should be recorded after failed connect");
        AssertTrue(errors.Any(e => e.Contains("Connection failed", StringComparison.OrdinalIgnoreCase) || e.Contains("not found", StringComparison.OrdinalIgnoreCase)),
            "Error message should mention connection failure or not found");
    }

    private async Task TestGetErrorsAsync_ReturnsCollectedErrors()
    {
        var adapter = new DesktopAdapter(null);
        var errors = await adapter.GetErrorsAsync(CancellationToken.None);
        AssertNotNull(errors);
        AssertEqual(0, errors.Count);
    }

    private async Task TestIsConnected_BeforeConnect_IsFalse()
    {
        var adapter = new DesktopAdapter(null);
        AssertFalse(adapter.IsConnected);
        await Task.CompletedTask;
    }
}
