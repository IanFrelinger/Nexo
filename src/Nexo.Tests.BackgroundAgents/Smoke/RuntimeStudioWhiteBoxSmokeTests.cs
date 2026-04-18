using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Forge;
using Nexo.BackgroundAgents.Objectives;
using Nexo.BackgroundAgents.Observations;
using Nexo.Policies.Dev;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Smoke;

/// <summary>
/// White-box smoke: exercises runtime-studio stores and forge policy in-process
/// (no CLI, no daemon). Complements <see cref="Nexo.Tests.Infrastructure.Tests.CLI.RuntimeStudioBlackBoxSmokeTests"/>.
/// </summary>
[Trait("Category", "Smoke")]
[Trait("Category", "RuntimeStudio")]
public sealed class RuntimeStudioWhiteBoxSmokeTests : IDisposable
{
    private readonly string _root;

    public RuntimeStudioWhiteBoxSmokeTests()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-whitebox-smoke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        _root = path;
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void JsonlObservationStore_append_and_read_round_trip()
    {
        var path = Path.Combine(_root, "obs.jsonl");
        var store = new JsonlObservationStore(path);
        store.Append(new RuntimeObservation(
            DateTimeOffset.UtcNow,
            "whitebox",
            ObservationKind.Build,
            "smoke build",
            ObservationSeverity.Info,
            facts: new Dictionary<string, string> { ["objective_id"] = "wb-1" }));
        var rows = store.ReadSince().ToList();
        rows.Should().HaveCount(1);
        rows[0].source.Should().Be("whitebox");
        rows[0].facts!["objective_id"].Should().Be("wb-1");
    }

    [Fact]
    public void ObjectiveStore_add_claim_complete_lifecycle()
    {
        var store = new ObjectiveStore(Path.Combine(_root, "objectives"));
        store.Add(new ObjectiveDocument { Id = "wb-obj", Title = "White box" });
        var claimed = store.ClaimNext("agent-whitebox");
        claimed.Should().NotBeNull();
        claimed!.Status.Should().Be(ObjectiveStatus.InProgress);
        store.Complete("wb-obj", "done");
        store.Find("wb-obj")!.Status.Should().Be(ObjectiveStatus.Done);
    }

    [Fact]
    public void ChangeProposalStore_proposed_approve_round_trip()
    {
        var store = new ChangeProposalStore(Path.Combine(_root, "forge"));
        store.Add(new ChangeProposal
        {
            Id = "wb-pr",
            TargetPath = "src/Smoke.cs",
            NewContent = "// smoke",
            Summary = "white box proposal"
        });
        store.Approve("wb-pr", "ci", "lgtm");
        store.Find("wb-pr")!.Status.Should().Be(ChangeProposalStatus.Approved);
    }

    [Fact]
    public void ForgeMediatedWritesPolicy_passive_blocks_src_write_active_allows()
    {
        var passive = new ForgeMediatedWritesPolicy(() => BackgroundAgentAggressivenessMode.Passive);
        var write = new ToolCall("repo.fs.write", JsonSerializer.SerializeToElement(new { path = "src/Foo.cs", content = "x" }));
        var snap = new WorldSnapshot(0, new Dictionary<string, object?>());
        passive.Approve(write, snap, out var denyReason).Should().BeFalse();
        denyReason.Should().Contain("forge.propose_change");

        var active = new ForgeMediatedWritesPolicy(() => BackgroundAgentAggressivenessMode.Active);
        active.Approve(write, snap, out _).Should().BeTrue();
    }
}
