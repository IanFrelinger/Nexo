using FluentAssertions;
using Nexo.Core.Application.Trust.Models;
using Nexo.Infrastructure.Trust;
using System.Reflection;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Trust;

public sealed class AccessBoundaryGapCoverageTests
{
    [Fact]
    public void IsCategoryAllowed_and_IsSourceAllowed_default_to_true_when_unconfigured()
    {
        var boundary = new AccessBoundary();

        boundary.IsCategoryAllowed("shell").Should().BeTrue();
        boundary.IsSourceAllowed("git").Should().BeTrue();
    }

    [Fact]
    public void IsSourceAllowedForProject_uses_project_override_before_global_rule()
    {
        var boundary = new AccessBoundary();
        boundary.SetSourceAllowed("git", false);
        boundary.SetProjectOverride("/proj", new Dictionary<string, bool> { ["git"] = true });

        boundary.IsSourceAllowedForProject("git", "/proj").Should().BeTrue();
        boundary.IsSourceAllowedForProject("git", "/other").Should().BeFalse();
    }

    [Fact]
    public void IsSourceAllowedForProject_falls_back_when_project_has_no_matching_source()
    {
        var boundary = new AccessBoundary();
        boundary.SetSourceAllowed("git", false);
        boundary.SetProjectOverride("/proj", new Dictionary<string, bool> { ["npm"] = true });

        boundary.IsSourceAllowedForProject("git", "/proj").Should().BeFalse();
    }

    [Fact]
    public void SetPause_raises_boundary_changed_and_skips_duplicate_updates()
    {
        var boundary = new AccessBoundary();
        var events = new List<BoundaryChangeEvent>();
        boundary.BoundaryChanged += events.Add;

        boundary.SetPause(true);
        boundary.SetPause(true);

        events.Should().ContainSingle(e =>
            e.ChangeType == "pause" &&
            e.PreviousState == "resumed" &&
            e.NewState == "paused");
        boundary.IsObservationPaused.Should().BeTrue();
    }

    [Fact]
    public void SetCategoryAllowed_raises_event_with_previous_state_and_skips_no_op()
    {
        var boundary = new AccessBoundary();
        var events = new List<BoundaryChangeEvent>();
        boundary.BoundaryChanged += events.Add;

        boundary.SetCategoryAllowed("shell", false);
        boundary.SetCategoryAllowed("shell", false);

        events.Should().ContainSingle(e =>
            e.ChangeType == "category" &&
            e.Category == "shell" &&
            e.PreviousState == null &&
            e.NewState == "denied");
    }

    [Fact]
    public void SetCategoryAllowed_trims_key_and_uses_case_insensitive_storage()
    {
        var boundary = new AccessBoundary();
        boundary.SetCategoryAllowed("  Shell  ", false);

        boundary.IsCategoryAllowed("shell").Should().BeFalse();
        boundary.GetCategoryAllowlistSnapshot().Should().ContainKey("Shell");
    }

    [Fact]
    public void SetSourceAllowed_raises_event_when_toggling_existing_value()
    {
        var boundary = new AccessBoundary();
        var events = new List<BoundaryChangeEvent>();
        boundary.BoundaryChanged += events.Add;

        boundary.SetSourceAllowed("git", false);
        boundary.SetSourceAllowed("git", true);

        events.Should().HaveCount(2);
        events[1].ChangeType.Should().Be("source");
        events[1].PreviousState.Should().Be("denied");
        events[1].NewState.Should().Be("allowed");
    }

    [Fact]
    public void SetProjectOverride_raises_event_when_clearing_overrides()
    {
        var boundary = new AccessBoundary();
        var events = new List<BoundaryChangeEvent>();
        boundary.BoundaryChanged += events.Add;

        boundary.SetProjectOverride("/proj", new Dictionary<string, bool> { ["git"] = true });
        boundary.SetProjectOverride("/proj", null);

        events.Should().HaveCount(2);
        events[1].ChangeType.Should().Be("project");
        events[1].NewState.Should().Be("overrides cleared");
    }

    [Fact]
    public void Reset_clears_state_and_raises_reset_event()
    {
        var boundary = new AccessBoundary();
        var events = new List<BoundaryChangeEvent>();
        boundary.BoundaryChanged += events.Add;

        boundary.SetPause(true);
        boundary.SetCategoryAllowed("shell", false);
        boundary.ApplyPolicyPack(CreatePack("strict"));
        boundary.Reset();

        boundary.IsObservationPaused.Should().BeFalse();
        boundary.GetActivePolicyPack().Should().BeNull();
        boundary.GetCategoryAllowlistSnapshot().Should().BeEmpty();
        events.Should().Contain(e => e.ChangeType == "reset" && e.NewState == "default");
    }

    [Fact]
    public void ApplyPolicyPack_defaults_blank_version_and_records_previous_pack()
    {
        var boundary = new AccessBoundary();
        var events = new List<BoundaryChangeEvent>();
        boundary.BoundaryChanged += events.Add;

        boundary.ApplyPolicyPack(CreatePack("first", version: "1.2.3"));
        boundary.ApplyPolicyPack(CreatePack("second", version: ""));

        boundary.GetActivePolicyPack()!.Version.Should().Be("1.0.0");
        events.Should().Contain(e =>
            e.ChangeType == "policy-pack" &&
            e.PreviousState == "first@1.2.3" &&
            e.NewState == "second@1.0.0");
    }

    [Fact]
    public void ApplyPolicyPack_rejects_blank_pack_id()
    {
        var boundary = new AccessBoundary();
        var pack = CreatePack("  ");

        var act = () => boundary.ApplyPolicyPack(pack);

        act.Should().Throw<ArgumentException>().WithParameterName("pack");
    }

    [Fact]
    public void GetSourceAllowlistSnapshot_returns_copy()
    {
        var boundary = new AccessBoundary();
        boundary.SetSourceAllowed("git", false);

        var snapshot = boundary.GetSourceAllowlistSnapshot();
        snapshot.TryGetValue("git", out var before).Should().BeTrue();
        before.Should().BeFalse();

        boundary.SetSourceAllowed("git", true);

        snapshot.TryGetValue("git", out var after).Should().BeTrue();
        after.Should().BeFalse();
    }

    [Fact]
    public async Task Constructor_tolerates_invalid_config_file()
    {
        var path = Path.Combine(Path.GetTempPath(), "boundary-bad-" + Guid.NewGuid() + ".json");
        await File.WriteAllTextAsync(path, "{not json");
        try
        {
            var boundary = new AccessBoundary(path);

            boundary.IsObservationPaused.Should().BeFalse();
            boundary.IsCategoryAllowed("shell").Should().BeTrue();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task SaveToFileAsync_is_no_op_when_config_path_is_null()
    {
        var boundary = new AccessBoundary();
        boundary.SetPause(true);

        await InvokeSaveToFileAsync(boundary);

        boundary.IsObservationPaused.Should().BeTrue();
    }

    private static TrustPolicyPack CreatePack(string id, string version = "1.0.0")
        => new(
            id,
            version,
            "Display",
            "Description",
            PauseObservationByDefault: true,
            new Dictionary<string, bool> { ["shell"] = false },
            new Dictionary<string, bool> { ["git"] = false },
            Array.Empty<TrustPolicyPackProjectRule>());

    private static async Task InvokeSaveToFileAsync(AccessBoundary boundary)
    {
        var save = typeof(AccessBoundary).GetMethod(
            "SaveToFileAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        save.Should().NotBeNull();
        await ((Task)save!.Invoke(boundary, null)!).ConfigureAwait(false);
    }
}
