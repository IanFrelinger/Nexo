using System.Diagnostics;
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
            await TestAssessDemo_DotnetProbeTracksAspNetCoreRuntimeAsync(cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// The dotnet probe must reflect whether the CLI can actually run, not just whether an SDK is on PATH:
    /// Nexo.CLI/Nexo.API target net8.0 and need Microsoft.AspNetCore.App 8.x, or 9.x+ via RollForward=Major.
    /// Compare the probe against ground truth from `dotnet --list-runtimes` on this host.
    /// </summary>
    private async Task TestAssessDemo_DotnetProbeTracksAspNetCoreRuntimeAsync(CancellationToken ct)
    {
        var assessment = await BootstrapRuntime.AssessDemoAsync("demo", includeOptional: false, ct).ConfigureAwait(false);
        var dotnet = assessment.Dependencies.First(d => string.Equals(d.Id, "dotnet", StringComparison.OrdinalIgnoreCase));

        var psi = new ProcessStartInfo("dotnet", "--list-runtimes")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(psi);
        AssertTrue(process is not null, "expected `dotnet --list-runtimes` to start");
        var stdout = await process!.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);

        var hasRunnableAspNetCore = stdout
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("Microsoft.AspNetCore.App ", StringComparison.Ordinal))
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1].Split('.')[0])
            .Any(major => int.TryParse(major, out var m) && m >= 8);

        AssertEqual(hasRunnableAspNetCore, dotnet.Installed,
            $"dotnet probe should report installed={hasRunnableAspNetCore} given `dotnet --list-runtimes`:{Environment.NewLine}{stdout}");
    }
}
