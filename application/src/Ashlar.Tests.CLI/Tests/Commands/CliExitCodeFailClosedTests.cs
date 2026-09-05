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
}
