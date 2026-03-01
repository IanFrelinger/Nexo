using FluentAssertions;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Infrastructure.Analysis.BrickAnalyzer;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Analysis;

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
                public class Bad
                {
                    public void M()
                    {
                        try { }
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
                public class Good
                {
                    public void M()
                    {
                        try { }
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
