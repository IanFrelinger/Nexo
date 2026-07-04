using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

/// <summary>Tests for empty catch code fixer.</summary>
[Trait("Category", "Adaptation")]
public sealed class EmptyCatchCodeFixerTests
{
    [Fact]
    public async Task TryFixAsync_EmptyCatch_AddsTraceWriteLine()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "nexo-emptycatch-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "Test.cs");
            var source = """
                namespace Test;
                /// <summary>Tests for c.</summary>
                public class C
                {
                    public void M()
                    {
                        try { }
                        /// <summary>Catch.</summary>
                        catch (Exception) { }
                    }
                }
                """;
            await File.WriteAllTextAsync(filePath, source);

            var services = new ServiceCollection()
                .AddAdaptationInfrastructure()
                .BuildServiceProvider();
            var fixer = services.GetRequiredService<ISourceCodeFixer>();

            var violation = new Violation
            {
                Rule = "EmptyCatch",
                Message = "Empty catch block",
                FilePath = filePath,
                LineNumber = null, // Let fixer find first empty catch
            };

            var fixed_ = await fixer.TryFixAsync(violation);
            Assert.True(fixed_);

            var result = await File.ReadAllTextAsync(filePath);
            Assert.Contains("Trace.WriteLine", result);
            Assert.Contains("Caught", result);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task TryFixAsync_NonEmptyCatchRule_ReturnsFalse()
    {
        var services = new ServiceCollection()
            .AddAdaptationInfrastructure()
            .BuildServiceProvider();
        var fixer = services.GetRequiredService<ISourceCodeFixer>();

        var violation = new Violation
        {
            Rule = "BlockingInAsync",
            Message = "Blocking call",
            FilePath = "x.cs",
        };

        var fixed_ = await fixer.TryFixAsync(violation);
        Assert.False(fixed_);
    }
}
