using FluentAssertions;
using Nexo.BackgroundAgents.Forge;
using Nexo.CLI.Commands.BackgroundAgent;
using Xunit;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>Tests for runtime studio operator dashboard summary.</summary>
public sealed class RuntimeStudioOperatorDashboardSummaryTests : IDisposable
{
    private readonly string _root;

    public RuntimeStudioOperatorDashboardSummaryTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "nexo-dash-sum-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void BuildJson_counts_objectives_proposals_and_includes_paths()
    {
        var objRoot = Path.Combine(_root, "objectives");
        Directory.CreateDirectory(Path.Combine(objRoot, "pending"));
        Directory.CreateDirectory(Path.Combine(objRoot, "in_progress"));
        File.WriteAllText(Path.Combine(objRoot, "pending", "a.md"), "id: a\ntitle: t\n");
        File.WriteAllText(Path.Combine(objRoot, "in_progress", "b.md"), "id: b\ntitle: t\n");

        var forgeRoot = Path.Combine(_root, "forge");
        var forgeStore = new ChangeProposalStore(forgeRoot);
        forgeStore.Add(new ChangeProposal
        {
            Id = "x",
            TargetPath = "src/X.cs",
            NewContent = "//",
            Summary = "s"
        });

        var obsPath = Path.Combine(_root, "obs.jsonl");
        File.WriteAllText(obsPath, "{\"k\":1}\n");

        var modePath = Path.Combine(_root, "mode.json");
        File.WriteAllText(modePath, """{"Mode":"passive"}""");

        var paths = new RuntimeStudioOperatorDashboardSummary.PathsInfo(objRoot, forgeRoot, obsPath, modePath);
        var json = RuntimeStudioOperatorDashboardSummary.BuildJson(paths);

        json.Should().Contain("\"Pending\": 1");
        json.Should().Contain("\"InProgress\": 1");
        json.Should().Contain("\"Proposed\": 1");
        json.Should().Contain("Passive");
        json.Should().Contain("\"observationsTail\"");
        json.Should().Contain("\"metrics\"");
        json.Should().Contain("\"ObservationsFileBytes\"");
    }
}
