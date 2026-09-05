using FluentAssertions;
using Ashlar.BackgroundAgents.Objectives;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Objectives;

/// <summary>Tests for objective store.</summary>
public class ObjectiveStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ObjectiveStore _store;

    public ObjectiveStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ashlar-objectives-" + Guid.NewGuid().ToString("N"));
        _store = new ObjectiveStore(_tempDir, maxAttempts: 3);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void Add_writes_pending_file_under_pending_folder()
    {
        var doc = _store.Add(new ObjectiveDocument
        {
            Id = "first",
            Title = "First objective",
            Priority = 5
        });

        doc.Status.Should().Be(ObjectiveStatus.Pending);
        File.Exists(Path.Combine(_tempDir, "pending", "first.md")).Should().BeTrue();
        Directory.GetFiles(Path.Combine(_tempDir, "pending"), "*.md").Should().HaveCount(1);
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("../../outside")]
    [InlineData("nested/objective")]
    [InlineData(@"nested\objective")]
    [InlineData(".hidden")]
    [InlineData("name.")]
    [InlineData("CON")]
    [InlineData("LPT1.txt")]
    public void Add_rejects_nonportable_or_traversing_ids(string id)
    {
        var act = () => _store.Add(new ObjectiveDocument { Id = id, Title = "unsafe" });

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid objective id*");
    }

    [Fact]
    public void Add_rejects_absolute_id_without_writing_outside_store()
    {
        var escaped = Path.Combine(Path.GetTempPath(), "ashlar-objective-escape-" + Guid.NewGuid().ToString("N"));
        var act = () => _store.Add(new ObjectiveDocument { Id = escaped, Title = "unsafe" });

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid objective id*");
        File.Exists(escaped + ".md").Should().BeFalse();
    }

    [Fact]
    public void List_fails_closed_on_nonportable_objective_filename()
    {
        var pending = Path.Combine(_tempDir, "pending");
        Directory.CreateDirectory(pending);
        File.WriteAllText(Path.Combine(pending, "bad name.md"), "tampered");

        var act = () => _store.List(ObjectiveStatus.Pending);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid objective filename*");
    }

    [Fact]
    public void List_orders_by_priority_then_creation()
    {
        _store.Add(new ObjectiveDocument { Id = "low", Title = "L", Priority = 100, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5) });
        _store.Add(new ObjectiveDocument { Id = "high", Title = "H", Priority = 1, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1) });
        _store.Add(new ObjectiveDocument { Id = "mid", Title = "M", Priority = 50, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-3) });

        var pending = _store.List(ObjectiveStatus.Pending);
        pending.Select(o => o.Id).Should().Equal("high", "mid", "low");
    }

    [Fact]
    public void ClaimNext_moves_highest_priority_to_in_progress_and_increments_attempts()
    {
        _store.Add(new ObjectiveDocument { Id = "a", Title = "A", Priority = 10 });
        _store.Add(new ObjectiveDocument { Id = "b", Title = "B", Priority = 1 });

        var claimed = _store.ClaimNext("runtime-planner");
        claimed.Should().NotBeNull();
        claimed!.Id.Should().Be("b");
        claimed.Status.Should().Be(ObjectiveStatus.InProgress);
        claimed.Owner.Should().Be("runtime-planner");
        claimed.Attempts.Should().Be(1);

        File.Exists(Path.Combine(_tempDir, "in_progress", "b.md")).Should().BeTrue();
        File.Exists(Path.Combine(_tempDir, "pending", "b.md")).Should().BeFalse();
    }

    [Fact]
    public void ClaimNext_returns_null_when_pending_is_empty()
    {
        _store.ClaimNext("runtime-planner").Should().BeNull();
    }

    [Fact]
    public void Complete_moves_in_progress_to_done_and_appends_note()
    {
        _store.Add(new ObjectiveDocument { Id = "c", Title = "C", Body = "original body" });
        _store.ClaimNext("runtime-planner");

        var done = _store.Complete("c", note: "fixed by PR #42");

        done.Status.Should().Be(ObjectiveStatus.Done);
        done.Body.Should().Contain("original body");
        done.Body.Should().Contain("fixed by PR #42");
        File.Exists(Path.Combine(_tempDir, "done", "c.md")).Should().BeTrue();
        File.Exists(Path.Combine(_tempDir, "in_progress", "c.md")).Should().BeFalse();
    }

    [Fact]
    public void Complete_throws_when_objective_is_not_in_progress()
    {
        _store.Add(new ObjectiveDocument { Id = "x", Title = "X" });
        var act = () => _store.Complete("x");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*expected InProgress*");
    }

    [Fact]
    public void Block_moves_to_blocked_and_records_reason()
    {
        _store.Add(new ObjectiveDocument { Id = "stuck", Title = "Stuck" });
        _store.ClaimNext("runtime-planner");

        var blocked = _store.Block("stuck", "tester is offline");
        blocked.Status.Should().Be(ObjectiveStatus.Blocked);
        blocked.BlockedReason.Should().Be("tester is offline");
        File.Exists(Path.Combine(_tempDir, "blocked", "stuck.md")).Should().BeTrue();
    }

    [Fact]
    public void Unblock_clears_reason_and_resets_attempts()
    {
        _store.Add(new ObjectiveDocument { Id = "y", Title = "Y" });
        _store.ClaimNext("runtime-planner");
        _store.Block("y", "transient");

        var revived = _store.Unblock("y");
        revived.Status.Should().Be(ObjectiveStatus.Pending);
        revived.BlockedReason.Should().BeNull();
        revived.Attempts.Should().Be(0);
    }

    [Fact]
    public void ClaimNext_auto_blocks_objective_at_max_attempts()
    {
        // Pre-seed a file that already shows three failed attempts (== maxAttempts).
        var alreadyExhausted = new ObjectiveDocument
        {
            Id = "burned",
            Title = "Already retried",
            Priority = 1,
            Attempts = 3,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        var path = Path.Combine(_tempDir, "pending", "burned.md");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, alreadyExhausted.Serialize());

        _store.Add(new ObjectiveDocument { Id = "fresh", Title = "Fresh", Priority = 5 });

        var claimed = _store.ClaimNext("runtime-planner");
        claimed!.Id.Should().Be("fresh", "burned should auto-block on attempt 4");

        File.Exists(Path.Combine(_tempDir, "blocked", "burned.md")).Should().BeTrue();
        File.Exists(Path.Combine(_tempDir, "pending", "burned.md")).Should().BeFalse();
        var burnedDoc = _store.Find("burned")!;
        burnedDoc.Status.Should().Be(ObjectiveStatus.Blocked);
        burnedDoc.BlockedReason.Should().Be("max_attempts_reached");
    }

    [Fact]
    public void Find_locates_objective_in_any_status_folder()
    {
        _store.Add(new ObjectiveDocument { Id = "z", Title = "Z" });
        _store.ClaimNext("runtime-planner");
        _store.Find("z")!.Status.Should().Be(ObjectiveStatus.InProgress);

        _store.Complete("z");
        _store.Find("z")!.Status.Should().Be(ObjectiveStatus.Done);
    }
}
