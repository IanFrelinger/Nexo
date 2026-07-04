using System.Text.Json;
using Nexo.CLI.Commands;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>Tests for doctor command.</summary>
public sealed class DoctorCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestRunAsyncReturnsExitCode().ConfigureAwait(false);
            await TestJsonOutputContainsOkField().ConfigureAwait(false);
            await TestJsonOutputIncludesRemediationSection().ConfigureAwait(false);
            return new TestResult
            {
                Name = nameof(DoctorCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "Doctor command tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(DoctorCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(DoctorCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestRunAsyncReturnsExitCode()
    {
        var exitCode = await DoctorCommand.ExecuteAsync(
            "demo",
            includeOptional: false,
            json: false,
            fix: false,
            dryRun: false,
            autoApproveFixes: false,
            CancellationToken.None).ConfigureAwait(false);
        /// <summary>Assert equal.</summary>
        /// <param name="succeed."">Succeed.".</param>
        AssertEqual(0, exitCode, "Doctor should pass when bootstrap deps and CLI smoke succeed.");
    }

    private async Task TestJsonOutputContainsOkField()
    {
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            var exitCode = await DoctorCommand.ExecuteAsync(
                "demo",
                includeOptional: false,
                json: true,
                fix: false,
                dryRun: false,
                autoApproveFixes: false,
                CancellationToken.None).ConfigureAwait(false);
            /// <summary>Assert equal.</summary>
            /// <param name="pass."">Pass.".</param>
            AssertEqual(0, exitCode, "Doctor JSON mode should exit 0 when checks pass.");
            var output = writer.ToString();
            AssertTrue(output.Contains("\"ok\"", StringComparison.OrdinalIgnoreCase), "Doctor JSON output should include 'ok'.");
            AssertTrue(output.Contains("\"checks\"", StringComparison.OrdinalIgnoreCase), "Doctor JSON output should include 'checks'.");
            using var doc = JsonDocument.Parse(output.Trim());
            AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean(), "Doctor JSON ok should be true when the host is healthy.");
            AssertTrue(
                doc.RootElement.GetProperty("checks").GetProperty("dependencyCheck").GetProperty("supported").GetBoolean(),
                "dependencyCheck.supported should be true on supported hosts.");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private async Task TestJsonOutputIncludesRemediationSection()
    {
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            var exitCode = await DoctorCommand.ExecuteAsync(
                "demo",
                includeOptional: false,
                json: true,
                fix: true,
                dryRun: true,
                autoApproveFixes: false,
                CancellationToken.None).ConfigureAwait(false);
            /// <summary>Assert equal.</summary>
            /// <param name="pass."">Pass.".</param>
            AssertEqual(0, exitCode, "Doctor JSON with --fix --dry-run should exit 0 when checks pass.");
            var output = writer.ToString();
            AssertTrue(output.Contains("\"remediation\"", StringComparison.OrdinalIgnoreCase), "Doctor JSON output should include remediation section.");
            AssertTrue(output.Contains("\"fixEnabled\"", StringComparison.OrdinalIgnoreCase), "Doctor JSON remediation should include fixEnabled.");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
