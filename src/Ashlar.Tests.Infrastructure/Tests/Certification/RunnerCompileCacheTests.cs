using FluentAssertions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Helpers;
using Microsoft.CodeAnalysis;
using Xunit;
using RunnerCompileCache = Ashlar.Infrastructure.Certification.LocalProcessExecutionBackend.RunnerCompileCache;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The witness replay runner is compiled once per reference set per process, and never shared
/// across a different one.
///
/// <para><b>Why this exists.</b> Every certification creates two
/// <see cref="LocalProcessExecutionBackend"/>s — one for the candidate, one inside the mutation
/// engine — and each compiled the runner from scratch: the same source against the same
/// references, one to two seconds of Roslyn, twice per certification, hundreds of times per
/// cert-gate run. The cache removes the repeat. What must not move is the runner's identity: a
/// hit has to be the image a fresh compile would have produced, so the key is the complete input
/// of the compile — source text, hosting runtime, and every reference by path AND module version
/// id — and the tests here pin each of those inputs as a separate reason to recompile.</para>
///
/// <para>The observable is <see cref="RunnerCompileCache.CompilesFor"/>, a per-key count. A
/// process-wide count would flake here: other test collections compile the runner against other
/// reference sets at the same time, and this assembly runs collections in parallel. Every test
/// below salts the runner source with a fresh GUID comment so its key is new to the process and
/// its first materialisation is a compile it can see.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class RunnerCompileCacheTests : IDisposable
{
    /// <summary>The set every in-memory certification in this assembly compiles against.</summary>
    private static readonly IReadOnlyList<string> BrickReferences =
    [
        typeof(Ashlar.Core.Domain.Bricks.Brick).Assembly.Location,
        typeof(BrickInput).Assembly.Location,
    ];

    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "ashlar-runner-cache-tests", Guid.NewGuid().ToString("N"));

    public RunnerCompileCacheTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [Fact(Timeout = TestTimeouts.Integration)]
    public async Task Two_backends_over_the_same_reference_set_compile_the_runner_once()
    {
        // Two certifications' worth of backends. Which of the two compiles depends on whether an
        // earlier test in this process already cached this set; that the SECOND does not is the claim.
        using var first = LocalProcessExecutionBackend.CreateForMutantsOnly(BrickReferences, CandidateExecutionLimits.Default);
        using var second = LocalProcessExecutionBackend.CreateForMutantsOnly(BrickReferences, CandidateExecutionLimits.Default);

        var firstRunner = await first.EnsureRunnerAsync(CancellationToken.None);
        var key = RunnerCompileCache.ComputeKey(
            WitnessReplayRunner.Source, LocalProcessExecutionBackend.BuildRunnerReferences(BrickReferences));
        var compilesAfterFirst = RunnerCompileCache.CompilesFor(key);

        var secondRunner = await second.EnsureRunnerAsync(CancellationToken.None);

        first.RunnerCompiledFresh.Should().NotBeNull("the backend records how it got its runner");
        second.RunnerCompiledFresh.Should().BeFalse(
            "the second backend over an identical reference set takes the runner the process already compiled");
        RunnerCompileCache.CompilesFor(key).Should().Be(compilesAfterFirst,
            "the second backend must not have compiled the runner again");
        compilesAfterFirst.Should().BeGreaterThan(0, "somebody in this process compiled this key exactly once, ever");

        secondRunner.Should().NotBe(firstRunner, "each backend still owns its runner image in its own scratch directory");
        File.ReadAllBytes(secondRunner).Should().Equal(File.ReadAllBytes(firstRunner),
            "a hit is a byte-for-byte copy of the compile this process already did, not a re-emit with a new MVID");
        File.Exists(Path.ChangeExtension(secondRunner, ".runtimeconfig.json")).Should().BeTrue(
            "the runtime config is written beside the runner on hits too; the host cannot launch the image without it");
    }

    [Fact(Timeout = TestTimeouts.Integration)]
    public Task A_changed_runner_source_recompiles_and_the_identical_source_is_then_served_from_the_cache()
    {
        var references = LocalProcessExecutionBackend.BuildRunnerReferences(BrickReferences);
        var source = Salted(WitnessReplayRunner.Source);
        var key = RunnerCompileCache.ComputeKey(source, references);
        RunnerCompileCache.CompilesFor(key).Should().Be(0, "the salt makes this key new to the process");

        RunnerCompileCache.Materialize(source, references, Dll("a"), CancellationToken.None)
            .Should().BeTrue("a key the process has never seen is a compile");
        RunnerCompileCache.Materialize(source, references, Dll("b"), CancellationToken.None)
            .Should().BeFalse("the identical inputs are a hit");
        RunnerCompileCache.CompilesFor(key).Should().Be(1);
        File.ReadAllBytes(Dll("b")).Should().Equal(File.ReadAllBytes(Dll("a")));

        var changed = source + "// one more character of runner source\n";
        var changedKey = RunnerCompileCache.ComputeKey(changed, references);
        changedKey.Should().NotBe(key, "the source text is part of the key");
        RunnerCompileCache.Materialize(changed, references, Dll("c"), CancellationToken.None)
            .Should().BeTrue("a changed runner source is another compile, however small the change");
        RunnerCompileCache.CompilesFor(changedKey).Should().Be(1);
        RunnerCompileCache.CompilesFor(key).Should().Be(1, "the original key was not recompiled by the change");

        return Task.CompletedTask;
    }

    [Fact(Timeout = TestTimeouts.Integration)]
    public Task A_different_reference_set_is_another_key_and_recompiles()
    {
        var source = Salted(WitnessReplayRunner.Source);
        var baseline = LocalProcessExecutionBackend.BuildRunnerReferences(BrickReferences);

        // One more reference: a copy of a managed assembly at a path no other test uses, so the
        // set differs from the baseline by exactly one entry.
        var extraPath = Path.Combine(_root, "extra", "Ashlar.Speed.Extra.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(extraPath)!);
        File.Copy(typeof(FactAttribute).Assembly.Location, extraPath);
        var widened = baseline.Append(MetadataReference.CreateFromFile(extraPath)).ToList();

        var baselineKey = RunnerCompileCache.ComputeKey(source, baseline);
        var widenedKey = RunnerCompileCache.ComputeKey(source, widened);
        widenedKey.Should().NotBe(baselineKey, "every reference path is part of the key");

        RunnerCompileCache.Materialize(source, baseline, Dll("base"), CancellationToken.None).Should().BeTrue();
        RunnerCompileCache.Materialize(source, widened, Dll("wide"), CancellationToken.None)
            .Should().BeTrue("the same source against a different reference set is a compile, never a reuse");
        RunnerCompileCache.Materialize(source, widened, Dll("wide2"), CancellationToken.None)
            .Should().BeFalse("the widened set is then cached under its own key");

        RunnerCompileCache.CompilesFor(baselineKey).Should().Be(1);
        RunnerCompileCache.CompilesFor(widenedKey).Should().Be(1);

        return Task.CompletedTask;
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public Task A_rebuilt_assembly_at_the_same_path_is_another_key()
    {
        // The brick's own references can be rebuilt between certifications with the path unchanged.
        // The compiler stamps a fresh MVID into every emit, so the MVID — not the path — says which
        // build the runner was compiled against.
        var path = Path.Combine(_root, "rebuilt", "Same.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        File.Copy(typeof(FactAttribute).Assembly.Location, path);
        var firstMvid = RunnerCompileCache.MvidOf(path);
        var firstKey = RunnerCompileCache.ComputeKey(WitnessReplayRunner.Source, References(path));

        File.Copy(typeof(FluentAssertions.AssertionExtensions).Assembly.Location, path, overwrite: true);
        var secondMvid = RunnerCompileCache.MvidOf(path);
        var secondKey = RunnerCompileCache.ComputeKey(WitnessReplayRunner.Source, References(path));

        secondMvid.Should().NotBe(firstMvid, "two different builds carry two different module version ids");
        secondKey.Should().NotBe(firstKey, "a rebuilt assembly at the same path must never hit the earlier build's runner");

        var mvidOfTheFileAsItIsNow = RunnerCompileCache.MvidOf(path);
        RunnerCompileCache.ComputeKey(WitnessReplayRunner.Source, References(path)).Should().Be(secondKey,
            "the key is deterministic over the same bytes (mvid {0})", mvidOfTheFileAsItIsNow);

        static List<MetadataReference> References(string extra) =>
            LocalProcessExecutionBackend.BuildRunnerReferences(BrickReferences)
                .Append(MetadataReference.CreateFromFile(extra))
                .ToList();

        return Task.CompletedTask;
    }

    [Fact(Timeout = TestTimeouts.Integration)]
    public Task Concurrent_backends_over_one_new_key_compile_once_and_every_one_gets_the_runner()
    {
        // A cert-gate run starts many backends over the same set at the same moment. Own threads,
        // not the pool: five of them block on the per-key gate for the length of one compile, and
        // the pool is shared with every other collection running beside this one.
        const int Racers = 6;
        var source = Salted(WitnessReplayRunner.Source);
        var references = LocalProcessExecutionBackend.BuildRunnerReferences(BrickReferences);
        var key = RunnerCompileCache.ComputeKey(source, references);

        var results = new bool[Racers];
        var failures = new Exception?[Racers];
        var startTogether = new Barrier(Racers);
        var threads = Enumerable.Range(0, Racers).Select(i => new Thread(() =>
        {
            try
            {
                startTogether.SignalAndWait();
                results[i] = RunnerCompileCache.Materialize(source, references, Dll($"racer{i}"), CancellationToken.None);
            }
            catch (Exception ex)
            {
                failures[i] = ex;
            }
        })
        { IsBackground = true, Name = $"runner-cache-racer-{i}" }).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join(TestTimeouts.Integration).Should().BeTrue("every racer finishes"));

        failures.Where(f => f is not null).Should().BeEmpty("no racer failed: {0}", string.Join("; ", failures.Where(f => f is not null)));
        results.Count(compiled => compiled).Should().Be(1, "exactly one racer compiled; the rest copied its image");
        RunnerCompileCache.CompilesFor(key).Should().Be(1);

        var expected = File.ReadAllBytes(Dll("racer0"));
        for (var i = 1; i < Racers; i++)
            File.ReadAllBytes(Dll($"racer{i}")).Should().Equal(expected, "racer {0} got the same image", i);

        RunnerCompileCache.CacheDirectory.Should().Contain(Environment.ProcessId.ToString(),
            "the cache is scoped to this process; nothing it compiled is offered to another");

        return Task.CompletedTask;
    }

    private string Dll(string name)
    {
        var path = Path.Combine(_root, name, WitnessReplayRunner.AssemblyName + ".dll");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }

    /// <summary>The runner source with a comment no other test in this process has used.</summary>
    private static string Salted(string source) =>
        source + "\n// runner-compile-cache test salt " + Guid.NewGuid().ToString("N") + "\n";
}
