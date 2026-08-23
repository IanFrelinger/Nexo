using FluentAssertions;
using Ashlar.BackgroundAgents.Forge;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Forge;

/// <summary>Tests for change proposal store.</summary>
public class ChangeProposalStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ChangeProposalStore _store;

    public ChangeProposalStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ashlar-forge-store-" + Guid.NewGuid().ToString("N"));
        _store = new ChangeProposalStore(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void Add_creates_file_in_proposed_folder_with_proposed_status()
    {
        var saved = _store.Add(NewProposal("p1"));
        saved.Status.Should().Be(ChangeProposalStatus.Proposed);
        File.Exists(Path.Combine(_tempDir, "proposed", "p1.json")).Should().BeTrue();
    }

    [Fact]
    public void Approve_then_apply_round_trips_through_folders()
    {
        _store.Add(NewProposal("p2"));
        _store.Approve("p2", "alice", "lgtm").Status.Should().Be(ChangeProposalStatus.Approved);
        File.Exists(Path.Combine(_tempDir, "approved", "p2.json")).Should().BeTrue();

        _store.MarkApplied("p2", "applied to disk").Status.Should().Be(ChangeProposalStatus.Applied);
        File.Exists(Path.Combine(_tempDir, "applied", "p2.json")).Should().BeTrue();
        File.Exists(Path.Combine(_tempDir, "approved", "p2.json")).Should().BeFalse();
    }

    [Fact]
    public void Reject_blocks_further_apply()
    {
        _store.Add(NewProposal("p3"));
        _store.Reject("p3", "bob", "wrong direction");
        var act = () => _store.MarkApplied("p3");
        act.Should().Throw<InvalidOperationException>().WithMessage("*expected Approved*");
    }

    [Fact]
    public void Stale_works_from_proposed_or_approved_but_is_no_op_on_terminal()
    {
        _store.Add(NewProposal("p4"));
        _store.MarkStale("p4").Status.Should().Be(ChangeProposalStatus.Stale);

        _store.Add(NewProposal("p5"));
        _store.Approve("p5");
        _store.MarkStale("p5").Status.Should().Be(ChangeProposalStatus.Stale);

        _store.Add(NewProposal("p6"));
        _store.Approve("p6");
        _store.MarkApplied("p6");
        _store.MarkStale("p6").Status.Should().Be(ChangeProposalStatus.Applied); // no-op
    }

    [Fact]
    public void List_filters_by_status_and_round_trip_preserves_metadata()
    {
        _store.Add(NewProposal("p7", target: "src/Foo.cs", summary: "alpha"));
        _store.Add(NewProposal("p8", target: "src/Bar.cs", summary: "beta"));
        _store.Approve("p8");

        var proposed = _store.List(ChangeProposalStatus.Proposed);
        var approved = _store.List(ChangeProposalStatus.Approved);
        proposed.Select(p => p.Id).Should().BeEquivalentTo(new[] { "p7" });
        approved.Select(p => p.Id).Should().BeEquivalentTo(new[] { "p8" });

        var loaded = _store.Find("p7")!;
        loaded.TargetPath.Should().Be("src/Foo.cs");
        loaded.Summary.Should().Be("alpha");
    }

    /// <summary>New proposal.</summary>
    /// <param name="id">Id.</param>
    /// <param name=""src/X.cs"">"src/x.cs".</param>
    /// <param name=""test"">"test".</param>
    private static ChangeProposal NewProposal(string id, string target = "src/X.cs", string summary = "test") =>
        new()
        {
            Id = id,
            TargetPath = target,
            NewContent = "// new",
            Summary = summary
        };
}
