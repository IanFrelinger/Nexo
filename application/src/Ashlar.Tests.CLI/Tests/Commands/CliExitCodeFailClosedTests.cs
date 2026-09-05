using System.CommandLine;
using Ashlar.CLI.Commands;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// #455: handlers that only set Environment.ExitCode lose the refusal —
/// System.CommandLine overwrites it back to 0 after the handler returns.
/// </summary>
public sealed class CliExitCodeFailClosedTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestComposeInvalidSupportLevelExitsNonZeroAsync().ConfigureAwait(false);
            await TestDockerPsWithoutEngineExitsNonZeroAsync().ConfigureAwait(false);
            await TestObserveEmptyWatchPathsExitsNonZeroAsync().ConfigureAwait(false);
            await TestIngestFailuresMissingTrxExitsNonZeroAsync().ConfigureAwait(false);
            await TestAdaptUnknownBrickExitsNonZeroAsync().ConfigureAwait(false);
            await TestAnalyzeBricksViolationExitsNonZeroAsync().ConfigureAwait(false);
            await TestImproveDryRunViolationExitsNonZeroAsync().ConfigureAwait(false);
            await TestImproveMissingStoreCreatesDirectoryWithoutStackTraceAsync().ConfigureAwait(false);
            await TestSelfContextInvalidLookbackExitsNonZeroWithoutStackTraceAsync().ConfigureAwait(false);
            await TestChangelogInvalidSinceExitsNonZeroWithoutStackTraceAsync().ConfigureAwait(false);
            return new TestResult
            {
                Name = nameof(CliExitCodeFailClosedTests),
                Category = "CLI",
                Passed = true,
                Message = "CLI exit-code fail-closed tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(CliExitCodeFailClosedTests),
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
                Name = nameof(CliExitCodeFailClosedTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestComposeInvalidSupportLevelExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new ComposeCommand());
            var exitCode = await root.InvokeAsync("compose --problem test --support-level not-a-level").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "Invalid --support-level must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestDockerPsWithoutEngineExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new DockerCommand());
            var exitCode = await root.InvokeAsync("docker ps").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "docker ps without an engine must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestObserveEmptyWatchPathsExitsNonZeroAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-observe-empty-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new ObserveCommand());
            var exitCode = await root.InvokeAsync($"observe --path {tempRoot} --duration 1s").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "observe with no watch paths must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private async Task TestIngestFailuresMissingTrxExitsNonZeroAsync()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"ashlar-no-trx-{Guid.NewGuid():N}");
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new IngestFailuresCommand());
            var exitCode = await root.InvokeAsync($"ingest-failures --trx-path {missing}").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "ingest-failures with no TRX files must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestAdaptUnknownBrickExitsNonZeroAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-adapt-missing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new AdaptCommand());
            var exitCode = await root.InvokeAsync($"adapt --brick not-a-brick --store-path {tempRoot}").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "adapt with an unknown brick must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private async Task TestAnalyzeBricksViolationExitsNonZeroAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-bricks-empty-catch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var source = Path.Combine(tempRoot, "Bad.cs");
        await File.WriteAllTextAsync(source, "class Bad { void M() { try { } catch { } } }").ConfigureAwait(false);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new AnalyzeBricksCommand());
            var exitCode = await root.InvokeAsync($"bricks --path {source}").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "analyze bricks with a violation must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private async Task TestImproveDryRunViolationExitsNonZeroAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-improve-dryrun-{Guid.NewGuid():N}");
        var stateDir = Path.Combine(tempRoot, "state");
        Directory.CreateDirectory(stateDir);
        var source = Path.Combine(tempRoot, "Bad.cs");
        await File.WriteAllTextAsync(source, "class Bad { void M() { try { } catch { } } }").ConfigureAwait(false);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new ImproveCommand());
            var exitCode = await root.InvokeAsync($"improve --dry-run --path {source} --store-path {stateDir} --yes").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "improve --dry-run with a violation must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private async Task TestImproveMissingStoreCreatesDirectoryWithoutStackTraceAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-improve-store-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var stateDir = Path.Combine(tempRoot, "state");
        var source = Path.Combine(tempRoot, "Empty.cs");
        await File.WriteAllTextAsync(source, "class C {}").ConfigureAwait(false);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new ImproveCommand());
            var exitCode = await root.InvokeAsync($"improve --dry-run --path {source} --store-path {stateDir} --yes").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode == 0, "improve --dry-run on clean source must exit 0 after creating --store-path.");
            AssertTrue(Directory.Exists(stateDir), "--store-path must be created instead of crashing.");
            AssertTrue(!output.Contains("DirectoryNotFoundException", StringComparison.Ordinal)
                && !output.Contains("   at ", StringComparison.Ordinal),
                "A missing --store-path must not print a stack trace.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private async Task TestSelfContextInvalidLookbackExitsNonZeroWithoutStackTraceAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new SelfContextCommand());
            var exitCode = await root.InvokeAsync("self-context --lookback xh").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "self-context --lookback xh must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --lookback", StringComparison.Ordinal),
                "An invalid --lookback must be refused legibly.");
            AssertTrue(!output.Contains("FormatException", StringComparison.Ordinal)
                && !output.Contains("   at ", StringComparison.Ordinal),
                "An invalid --lookback must not print a stack trace.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestChangelogInvalidSinceExitsNonZeroWithoutStackTraceAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new ChangelogCommand());
            var exitCode = await root.InvokeAsync("changelog --since xh").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "changelog --since xh must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --since", StringComparison.Ordinal),
                "An invalid --since must be refused legibly.");
            AssertTrue(!output.Contains("FormatException", StringComparison.Ordinal)
                && !output.Contains("   at ", StringComparison.Ordinal),
                "An invalid --since must not print a stack trace.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }
}
