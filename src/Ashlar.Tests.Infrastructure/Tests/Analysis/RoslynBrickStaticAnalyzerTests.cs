using FluentAssertions;
using Ashlar.Core.Application.Analysis.Ports;
using Ashlar.Infrastructure.Analysis.BrickAnalyzer;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Analysis;

/// <summary>Tests for roslyn brick static analyzer.</summary>
public class RoslynBrickStaticAnalyzerTests
{
    private readonly IBrickStaticAnalyzer _analyzer = new RoslynBrickStaticAnalyzer();

    [Fact]
    public async Task AnalyzeSourceAsync_EmptyCatch_ReportsViolation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"brick_analyzer_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var file = Path.Combine(tempDir, "Bad.cs");
            await File.WriteAllTextAsync(file, """
                /// <summary>Tests for bad.</summary>
                public class Bad
                {
                    public void M()
                    {
                        try { }
                        /// <summary>Catch.</summary>
                        catch (Exception) { }
                    }
                }
                """);

            var result = await _analyzer.AnalyzeSourceAsync(tempDir);

            result.Passed.Should().BeFalse();
            result.Violations.Should().Contain(v => v.Rule == "EmptyCatch");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task AnalyzeSourceAsync_CleanCode_Passes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"brick_analyzer_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var file = Path.Combine(tempDir, "Good.cs");
            await File.WriteAllTextAsync(file, """
                /// <summary>Tests for good.</summary>
                public class Good
                {
                    public void M()
                    {
                        try { }
                        /// <summary>Catch.</summary>
                        /// <param name="ex">Ex.</param>
                        catch (Exception ex) { throw; }
                    }
                }
                """);

            var result = await _analyzer.AnalyzeSourceAsync(tempDir);

            result.Passed.Should().BeTrue();
            result.Violations.Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
