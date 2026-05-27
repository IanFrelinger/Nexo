using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Infrastructure.Analysis.BrickAnalyzer;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Analysis;

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
                public class Bad
                {
                    public void M()
                    {
                        try { }
                        catch (System.Exception) { }
                    }
                }
                """);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "Ignored.cs"), """
                public class Ignored
                {
                    public void M()
                    {
                        try { }
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

            var result = await _analyzer.AnalyzeSourceAsync(tempDir);

            result.Passed.Should().BeFalse();
            result.Violations.Should().Contain(v => v.Rule == "ParseError");
        }
        finally
        {
            try { File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite); } catch { /* best effort */ }
            Cleanup(tempDir);
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
            Cleanup(tempDir);
        }
    }
}
