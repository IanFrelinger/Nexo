using System.Text.Json;
using CsCheck;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Forge;
using Nexo.BackgroundAgents.Objectives;
using Nexo.BackgroundAgents.Observations;
using Nexo.Policies.Dev;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Playground;

/// <summary>
/// In-process playground: property-style sampling over stores and forge policy with
/// many random inputs. Opt-in via <c>Category=Playground</c>.
/// </summary>
[Trait("Category", "Playground")]
[Trait("Category", "RuntimeStudio")]
public sealed class RuntimeStudioWhiteBoxPlaygroundTests
{
    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-wb-play-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void TryDelete(string root)
    {
        try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void JsonlObservationStore_random_batches_round_trip_and_kind_filter()
    {
        Gen.Int[1, 120].SelectMany(n =>
            Gen.Enum<ObservationKind>().SelectMany(kind =>
                Gen.Enum<ObservationSeverity>().SelectMany(sev =>
                    Gen.String[1, 12].SelectMany(src =>
                        Gen.String[1, 12].Select(sum => (n, kind, sev, src, sum)))))).Sample(t =>
        {
            var (n, kind, sev, src, sum) = t;
            var root = CreateTempRoot();
            try
            {
                var path = Path.Combine(root, "obs.jsonl");
                var store = new JsonlObservationStore(path);
                for (var i = 0; i < n; i++)
                {
                    store.Append(new RuntimeObservation(
                        DateTimeOffset.UtcNow.AddMilliseconds(-i),
                        $"{src}-{i}",
                        kind,
                        $"{sum}-{i}",
                        sev,
                        facts: i % 3 == 0
                            ? new Dictionary<string, string> { ["objective_id"] = $"obj-{i}" }
                            : null));
                }

                store.ReadSince().Should().HaveCount(n);
                store.ReadSince(kind: kind).Should().HaveCount(n);
                var limited = store.ReadSince(kind: kind, limit: Math.Min(7, n)).ToList();
                limited.Should().HaveCount(Math.Min(7, n));
                limited.Should().OnlyContain(o => o.kind == kind);
            }
            finally
            {
                TryDelete(root);
            }
        });
    }

    [Fact]
    public void ObjectiveStore_random_adds_list_and_find()
    {
        Gen.Int[1, 40].Sample(count =>
        {
            var root = CreateTempRoot();
            try
            {
                var store = new ObjectiveStore(Path.Combine(root, "objectives"));
                var ids = new List<string>();
                for (var i = 0; i < count; i++)
                {
                    var id = $"o-{i}-{Guid.NewGuid():N}";
                    ids.Add(id);
                    store.Add(new ObjectiveDocument
                    {
                        Id = id,
                        Title = $"title-{i}",
                        Priority = (i % 50) + 1,
                        Tags = i % 2 == 0 ? new[] { "a", "b" } : Array.Empty<string>()
                    });
                }

                store.List().Should().HaveCount(count);
                foreach (var id in ids)
                {
                    var found = store.Find(id);
                    found.Should().NotBeNull();
                    found!.Id.Should().Be(id);
                    found.Status.Should().Be(ObjectiveStatus.Pending);
                }
            }
            finally
            {
                TryDelete(root);
            }
        });
    }

    [Fact]
    public void ChangeProposalStore_random_queue_transitions()
    {
        Gen.Int[1, 25].Sample(n =>
        {
            var root = CreateTempRoot();
            try
            {
                var store = new ChangeProposalStore(Path.Combine(root, "forge"));
                var ids = new string[n];
                for (var i = 0; i < n; i++)
                {
                    ids[i] = $"p-{i}-{Guid.NewGuid():N}";
                    store.Add(new ChangeProposal
                    {
                        Id = ids[i],
                        TargetPath = i % 2 == 0 ? $"src/X{i}.cs" : $"docs/Y{i}.md",
                        NewContent = $"// {i}",
                        Summary = $"sum-{i}"
                    });
                }

                store.List().Should().HaveCount(n);
                foreach (var id in ids)
                {
                    store.Approve(id, "playground", "ok");
                    store.Find(id)!.Status.Should().Be(ChangeProposalStatus.Approved);
                }
            }
            finally
            {
                TryDelete(root);
            }
        });
    }

    [Fact]
    public void ForgeMediatedWritesPolicy_paths_and_modes_match_documented_rules()
    {
        var snap = new WorldSnapshot(0, new Dictionary<string, object?>());
        Gen.OneOf(
            Gen.Const("src/Foo.cs"),
            Gen.Const("tests/Bar.cs"),
            Gen.Const("SRC/z.cs"),
            Gen.Const("docs/readme.md"),
            Gen.Const(".nexo/scratch.txt"),
            Gen.Const("README.md")).SelectMany(rel =>
            Gen.OneOf(
                Gen.Const(BackgroundAgentAggressivenessMode.Passive),
                Gen.Const(BackgroundAgentAggressivenessMode.SemiActive),
                Gen.Const(BackgroundAgentAggressivenessMode.Active),
                Gen.Const(BackgroundAgentAggressivenessMode.Ambient))
                .Select(mode => (rel, mode))).Sample(pair =>
        {
            var (rel, mode) = pair;
            var policy = new ForgeMediatedWritesPolicy(() => mode);
            var write = new ToolCall("repo.fs.write", JsonSerializer.SerializeToElement(new { path = rel, content = "x" }));
            var replace = new ToolCall("repo.fs.search_replace", JsonSerializer.SerializeToElement(new { path = rel, oldText = "a", newText = "b" }));
            var lowTrust = mode is BackgroundAgentAggressivenessMode.Passive or BackgroundAgentAggressivenessMode.SemiActive;
            var norm = rel.Replace('\\', '/').TrimStart('/');
            var mediated = norm.StartsWith("src/", StringComparison.OrdinalIgnoreCase)
                || norm.StartsWith("tests/", StringComparison.OrdinalIgnoreCase);

            foreach (var call in new[] { write, replace })
            {
                var ok = policy.Approve(call, snap, out var reason);
                if (lowTrust && mediated)
                {
                    ok.Should().BeFalse($"{call.Id} {rel} {mode}");
                    reason.Should().Contain("forge.propose_change");
                }
                else
                {
                    ok.Should().BeTrue($"{call.Id} {rel} {mode}");
                }
            }
        });
    }

    [Fact]
    public void ForgeMediatedWritesPolicy_non_fs_tools_always_allowed_under_any_mode()
    {
        Gen.OneOf(
            Gen.Const(BackgroundAgentAggressivenessMode.Passive),
            Gen.Const(BackgroundAgentAggressivenessMode.Active)).Sample(mode =>
        {
            var policy = new ForgeMediatedWritesPolicy(() => mode);
            var snap = new WorldSnapshot(0, new Dictionary<string, object?>());
            var other = new ToolCall("forge.propose_change", JsonSerializer.SerializeToElement(new { path = "src/x.cs", content = "y" }));
            policy.Approve(other, snap, out var r).Should().BeTrue();
            r.Should().Be("OK");
        });
    }
}
