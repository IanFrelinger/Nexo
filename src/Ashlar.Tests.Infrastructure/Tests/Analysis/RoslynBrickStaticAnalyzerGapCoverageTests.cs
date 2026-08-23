using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Infrastructure.Analysis.BrickAnalyzer;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Analysis;

/// <summary>Tests for roslyn brick static analyzer gap coverage.</summary>
public sealed class RoslynBrickStaticAnalyzerGapCoverageTests
{
    private readonly RoslynBrickStaticAnalyzer _analyzer =
        new(NullLogger<RoslynBrickStaticAnalyzer>.Instance);

    [Fact]
    public async Task AnalyzeSourceAsync_blocking_in_async_reports_violation()
    {
        var tempDir = CreateTempDir();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "Blocking.cs"), """
                using System.Threading.Tasks;

                /// <summary>Tests for blocking.</summary>
                public class Blocking
                {
                    public async Task RunAsync()
                    {
                        var t = Task.FromResult(1);
                        t.GetAwaiter().GetResult();
                    }
                }
                """);

            var result = await _analyzer.AnalyzeSourceAsync(tempDir);

            result.Passed.Should().BeFalse();
            result.Violations.Should().Contain(v => v.Rule == "BlockingInAsync");
        }
        finally
        {
            /// <summary>Cleanup.</summary>
            Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task AnalyzeSourceAsync_dangerous_process_start_reports_violation()
    {
        var tempDir = CreateTempDir();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "ProcessStart.cs"), """
                using System.Diagnostics;

                /// <summary>Tests for proc.</summary>
                public class Proc
                {
                    public void Run()
                    {
                        Process.Start("echo");
                    }
                }
                """);

            var result = await _analyzer.AnalyzeSourceAsync(tempDir);

            result.Passed.Should().BeFalse();
            result.Violations.Should().Contain(v => v.Rule == "DangerousApi");
        }
        finally
        {
            /// <summary>Cleanup.</summary>
            Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task AnalyzeSourceAsync_single_file_path_analyzes_only_that_file()
    {
        var tempDir = CreateTempDir();
        try
        {
            var bad = Path.Combine(tempDir, "Bad.cs");
            await File.WriteAllTextAsync(bad, """
                /// <summary>Tests for bad.</summary>
                public class Bad
                {
                    public void M()
                    {
                        try { }
                        /// <summary>Catch.</summary>
                        catch (System.Exception) { }
                    }
                }
                """);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "Ignored.cs"), """
                /// <summary>Tests for ignored.</summary>
                public class Ignored
                {
                    public void M()
                    {
                        try { }
                        /// <summary>Catch.</summary>
                        catch (System.Exception) { }
                    }
                }
                """);

            var result = await _analyzer.AnalyzeSourceAsync(bad);

            result.Passed.Should().BeFalse();
            result.Violations.Should().ContainSingle(v => v.Rule == "EmptyCatch");
            result.Violations.Single().FilePath.Should().Be(Path.GetFullPath(bad));
        }
        finally
        {
            /// <summary>Cleanup.</summary>
            Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task AnalyzeSourceAsync_unreadable_file_reports_parse_error()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return;

        var tempDir = CreateTempDir();
        var file = Path.Combine(tempDir, "Unreadable.cs");
        try
        {
            await File.WriteAllTextAsync(file, "public class X {}");
            File.SetUnixFileMode(file, UnixFileMode.None);

            // Root bypasses DAC permission checks, so a mode-000 file is still readable and
            // this test's premise cannot hold. The dev container runs as root
            // (scripts/handoff/devbox.sh), which is why this failed there. Probe rather than
            // assume. NB: bails with `return`, matching the OS guard above, which xUnit
            // reports as PASSED — a green run under root is not evidence this path works.
            if (CanReadFile(file))
            {
                return;
            }

            var result = await _analyzer.AnalyzeSourceAsync(tempDir);

            result.Passed.Should().BeFalse();
            result.Violations.Should().Contain(v => v.Rule == "ParseError");
        }
        finally
        {
            try { File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite); } catch { /* best effort */ }
            /// <summary>Cleanup.</summary>
            Cleanup(tempDir);
        }
    }

    /// <summary>
    /// True when the file can actually be read, whatever its mode bits claim. Detects
    /// running with privileges that bypass permission checks (root), where an
    /// "unreadable file" test has no premise to stand on.
    /// </summary>
    private static bool CanReadFile(string path)
    {
        try
        {
            using var _ = File.OpenRead(path);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "roslyn-gap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void Cleanup(string dir)
    {
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public async Task AnalyzeSourceAsync_task_wait_in_async_method_reports_violation()
    {
        var tempDir = CreateTempDir();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "Wait.cs"), """
                using System.Threading.Tasks;

                /// <summary>Tests for waiter.</summary>
                public class Waiter
                {
                    public async Task RunAsync()
                    {
                        var t = Task.FromResult(1);
                        t.Wait();
                    }
                }
                """);

            var result = await _analyzer.AnalyzeSourceAsync(tempDir);

            result.Passed.Should().BeFalse();
            result.Violations.Should().Contain(v => v.Rule == "BlockingInAsync");
        }
        finally
        {
            /// <summary>Cleanup.</summary>
            Cleanup(tempDir);
        }
    }
}
