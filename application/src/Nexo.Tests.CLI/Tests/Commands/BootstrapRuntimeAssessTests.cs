using Nexo.CLI.Commands;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>
/// Validates <see cref="BootstrapRuntime.AssessDemoAsync"/> on the current host (including Windows probes).
/// </summary>
public sealed class BootstrapRuntimeAssessTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestAssessDemo_SupportedWithNoMissingRequiredAsync(cancellationToken).ConfigureAwait(false);
            await TestAssessDemo_IncludesCoreDependencyProbesAsync(cancellationToken).ConfigureAwait(false);
            return new TestResult
            {
                Name = nameof(BootstrapRuntimeAssessTests),
                Category = "CLI",
                Passed = true,
                Message = "Bootstrap runtime assess tests passed",
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(BootstrapRuntimeAssessTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(BootstrapRuntimeAssessTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
            };
        }
    }

    private async Task TestAssessDemo_SupportedWithNoMissingRequiredAsync(CancellationToken ct)
    {
        var assessment = await BootstrapRuntime.AssessDemoAsync("demo", includeOptional: false, ct).ConfigureAwait(false);
        AssertTrue(assessment.Supported, $"Bootstrap assess should be supported on this host (reason={assessment.Reason}).");
        AssertFalse(assessment.MissingRequired.Any(), "Expected no missing required demo dependencies.");
    }

    private async Task TestAssessDemo_IncludesCoreDependencyProbesAsync(CancellationToken ct)
    {
        var assessment = await BootstrapRuntime.AssessDemoAsync("demo", includeOptional: false, ct).ConfigureAwait(false);
        AssertTrue(assessment.Supported, "supported");
        var ids = assessment.Dependencies.Select(d => d.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        AssertTrue(ids.Contains("git"), "expected git dependency probe");
        AssertTrue(ids.Contains("dotnet"), "expected dotnet dependency probe");
        if (OperatingSystem.IsMacOS())
            AssertTrue(ids.Contains("brew"), "expected Homebrew dependency probe on macOS");
        else
            AssertTrue(ids.Contains("curl"), "expected curl dependency probe on Linux and Windows");
    }
}
