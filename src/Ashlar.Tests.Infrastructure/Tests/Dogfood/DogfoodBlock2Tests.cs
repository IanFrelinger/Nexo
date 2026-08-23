using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Analysis.Ports;
using Ashlar.Core.Application.Paths;
using Ashlar.Infrastructure.Analysis;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Block 2 dogfood gate: static analyzer must analyze Block 1 (Observation) code first.
/// The analyzer runs against the observation pipeline implementation and surfaces violations or confirms clean.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Dogfood")]
public sealed class DogfoodBlock2Tests
{
    [Fact(Timeout = 30000)]
    public async Task StaticAnalyzer_RunAgainstBlock1ObservationCode_CompletesAndSurfacesOrConfirms()
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var slnPath = Path.Combine(repoRoot, "Ashlar.sln");
        if (!File.Exists(slnPath))
            return;

        var block1Path = RepoPathResolver.FindBlock1ObservationPath(repoRoot);
        if (!Directory.Exists(block1Path))
            return;

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddCodeAnalyzers()
            .BuildServiceProvider();

        var analyzer = services.GetRequiredService<IBrickStaticAnalyzer>();
        var result = await analyzer.AnalyzeSourceAsync(block1Path, includeAnalyzers: false);

        // Gate: Analyzer ran against Block 1. Pass if it completed.
        // If violations exist, that's "actionable improvements"; if 0, code is clean.
        Assert.NotNull(result);
        if (result.TotalViolations > 0)
        {
            // Document at least one for the gate
            var first = result.Violations.First();
            Assert.False(string.IsNullOrEmpty(first.Rule), "Violation should have a rule name");
            Assert.False(string.IsNullOrEmpty(first.Message), "Violation should have a message");
        }
    }
}
