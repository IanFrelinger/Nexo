using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Analysis;
using Nexo.Infrastructure.Observation;
using Nexo.Infrastructure.SelfContext;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Phase F: Continuous self-improvement loop.
/// Validates the improve flow (analyze Block 1 path) completes on Nexo.
/// Uses same services as ImproveCommand; avoids CLI assembly-loading issues.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Dogfood")]
[Trait("Category", "Adaptation")]
public sealed class DogfoodClosedLoopTests : TempDirTestBase
{
    public DogfoodClosedLoopTests() : base("nexo-dogfood-closedloop") { }

    [Fact(Timeout = 30000)]
    public async Task ImproveFlow_AnalyzeBlock1Path_Completes()
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var slnPath = Path.Combine(repoRoot, "Nexo.sln");
        if (!File.Exists(slnPath))
        {
            return; // Not in Nexo repo
        }

        var block1Path = RepoPathResolver.FindBlock1ObservationPath(repoRoot);
        if (!Directory.Exists(block1Path))
        {
            return;
        }

        var storePath = Path.Combine(TempDir, "adapt.db");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddCodeAnalyzers()
            .AddAdaptationInfrastructure(storePath)
            .AddSelfContextInfrastructure(storePath)
            .BuildServiceProvider();

        var analyzer = services.GetRequiredService<IBrickStaticAnalyzer>();
        var analysisResult = await analyzer.AnalyzeSourceAsync(block1Path);

        // Gate: closed-loop analyze completes on Nexo Block 1 (violations or clean)
        Assert.NotNull(analysisResult);
    }
}
