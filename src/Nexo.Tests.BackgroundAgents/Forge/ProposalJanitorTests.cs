using FluentAssertions;
using Nexo.BackgroundAgents.Forge;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Forge;

public class ProposalJanitorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ChangeProposalStore _store;

    public ProposalJanitorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "nexo-forge-jan-" + Guid.NewGuid().ToString("N"));
        _store = new ChangeProposalStore(_tempDir);
    }

    public void Dispose() { try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ } }

    [Fact]
    public void Sweep_stales_old_proposed_and_old_approved_but_not_recent()
    {
        var now = DateTimeOffset.UtcNow;

        // Old proposed (created 100 hours ago) — should stale.
        var oldProposed = new ChangeProposal
        {
            Id = "old-prop",
            TargetPath = "src/A.cs",
            NewContent = "x",
            Summary = "old",
            CreatedAt = now.AddHours(-100),
            UpdatedAt = now.AddHours(-100)
        };
        _store.Add(oldProposed);
        // Force old UpdatedAt by re-saving (Add stamps fresh UpdatedAt).
        ForceTimestamp("old-prop", ChangeProposalStatus.Proposed, now.AddHours(-100));

        var freshProposed = _store.Add(new ChangeProposal
        {
            Id = "fresh-prop",
            TargetPath = "src/B.cs",
            NewContent = "x",
            Summary = "fresh"
        });

        var oldApproved = _store.Add(new ChangeProposal
        {
            Id = "old-app",
            TargetPath = "src/C.cs",
            NewContent = "x",
            Summary = "old approved"
        });
        _store.Approve("old-app");
        ForceTimestamp("old-app", ChangeProposalStatus.Approved, now.AddHours(-50));

        var janitor = new ProposalJanitor(
            _store,
            proposedTtl: TimeSpan.FromHours(72),
            approvedTtl: TimeSpan.FromHours(24),
            now: () => now);
        var staled = janitor.Sweep();

        staled.Should().BeEquivalentTo(new[] { "old-prop", "old-app" });
        _store.Find("old-prop")!.Status.Should().Be(ChangeProposalStatus.Stale);
        _store.Find("fresh-prop")!.Status.Should().Be(ChangeProposalStatus.Proposed);
        _store.Find("old-app")!.Status.Should().Be(ChangeProposalStatus.Stale);
    }

    private void ForceTimestamp(string id, ChangeProposalStatus status, DateTimeOffset stamp)
    {
        var path = Path.Combine(_tempDir, status.ToString().ToLowerInvariant(), id + ".json");
        var doc = System.Text.Json.JsonSerializer.Deserialize<ChangeProposal>(File.ReadAllText(path))!;
        var stamped = doc with { UpdatedAt = stamp, CreatedAt = stamp };
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(stamped, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }
}
