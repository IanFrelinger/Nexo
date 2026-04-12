using Nexo.CLI.Commands;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

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
            autoApproveFixes: false,
            CancellationToken.None).ConfigureAwait(false);
        AssertTrue(exitCode == 0 || exitCode == 1, "Doctor exit code should be deterministic (0 or 1).");
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
                autoApproveFixes: false,
                CancellationToken.None).ConfigureAwait(false);
            AssertTrue(exitCode == 0 || exitCode == 1, "Doctor JSON exit code should be deterministic (0 or 1).");
            var output = writer.ToString();
            AssertTrue(output.Contains("\"ok\"", StringComparison.OrdinalIgnoreCase), "Doctor JSON output should include 'ok'.");
            AssertTrue(output.Contains("\"checks\"", StringComparison.OrdinalIgnoreCase), "Doctor JSON output should include 'checks'.");
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
                autoApproveFixes: false,
                CancellationToken.None).ConfigureAwait(false);
            AssertTrue(exitCode == 0 || exitCode == 1, "Doctor JSON fix exit code should be deterministic (0 or 1).");
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
