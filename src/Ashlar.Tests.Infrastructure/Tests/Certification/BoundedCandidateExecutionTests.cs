using FluentAssertions;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Certification.HotSwap;
using Ashlar.Tests.Infrastructure.Certification.Reuse;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Author code — the candidate and every mutant of it — runs in a bounded child process, never
/// on the certifier's own threads, and the record says who killed what.
///
/// <para><b>Why this exists.</b> Two adjudicated blockers on the gate. CRITICAL: an honest brick
/// with a plain countdown loop never finished certifying, because the
/// <c>shift-relational-boundary</c> mutant turned <c>while (n &gt; 0)</c> into
/// <c>while (n &gt;= 0)</c> and nothing bounded mutant execution — the certifier spun forever
/// inside a reflective <c>Invoke</c>. HIGH: an honest brick with a recursive private helper
/// killed the certifier outright — a mutated literal made the recursion infinite, the stack
/// overflowed, and a <c>StackOverflowException</c> cannot be caught: exit 134, empty stdout, no
/// verdict, no message. Both fixtures are copied here verbatim from the adversarial oracles.</para>
///
/// <para><b>What the record must say.</b> A mutant the wall clock stopped or that killed its
/// process is dead — it can never certify — but the witness did not catch it, and a certificate
/// that files it under <c>killedMutants</c> claims teeth the witness never showed. So the record
/// carries <c>timedOutMutants</c> and <c>crashedMutants</c> as separate, signed lists, and the
/// three are disjoint and add up to <c>totalMutants</c> with the survivors.</para>
///
/// <para>Every test here spawns real child processes through the dotnet host running the test;
/// the timeouts are hang nets sized for a loaded runner, not budgets.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class BoundedCandidateExecutionTests
{
    /// <summary>
    /// Tight per-case budget so the timed-out cases finish in seconds; every other bound stays at
    /// its default. Recorded on the certificate, which the first test checks.
    /// </summary>
    private static readonly CandidateExecutionLimits TestLimits =
        CandidateExecutionLimits.Default with { PerCaseTimeout = TimeSpan.FromSeconds(2) };

    /// <summary>The /tmp/adv-mut/fx/hang-gt oracle: an honest digit counter with a countdown loop.</summary>
    private const string DigitCountBrickSource = """
using System.Linq;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Adv.Mut;

public sealed class DigitCountBrick : Brick
{
    public DigitCountBrick()
    {
        Id = "digits";
        Name = "digits";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "adversarial mutation fixture digits";
        Interface = new BrickInterface
        {
            Inputs = [ new BrickInputDefinition("value", "int", "value") ],
            Outputs = [ new BrickOutputDefinition("digits", "int", "digits") ]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var value = input.Get<int>("value");
        var n = value;
        var digits = 0;
        while (n > 0)
        {
            digits++;
            n /= 10;
        }

        var output = new BrickOutput();
        output.Set("digits", digits);
        return Task.FromResult(output);
    }

}
""";

    /// <summary>The /tmp/adv-mut/fx/digits.witness.json oracle, as the loader would normalise it.</summary>
    private static readonly WitnessSpec DigitsWitness = new(
        "digits",
        [
            Case(("value", 12345), ("digits", 5)),
            Case(("value", 7), ("digits", 1)),
            Case(("value", 100), ("digits", 3)),
            Case(("value", 0), ("digits", 0)),
        ]);

    /// <summary>
    /// The /tmp/skep-recur/DecBrick oracle: an honest factorial whose helper recurses through
    /// <c>Dec</c>. No operator swap is needed to make it fatal — <c>mutate-int-literal</c> on the
    /// <c>1</c> in <c>x - 1</c> alone makes the recursion infinite.
    /// </summary>
    private const string RecursiveFactorialBrickSource = """
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Skep.Recur;

public sealed class RecurBrick : Brick
{
    public RecurBrick()
    {
        Id = "skep-factorial";
        Name = "Factorial";
        Version = "1.0.0";
        Category = BrickCategory.Transform;
        Description = "Computes n! with a recursive helper.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("n", "int", "Non-negative integer")],
            Outputs = [new BrickOutputDefinition("factorial", "long", "n!")]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var n = input.Get<int>("n");
        var result = Factorial(n);
        var output = new BrickOutput { Summary = $"Factorial: {result}" };
        output.Set("factorial", result);
        return Task.FromResult(output);
    }

    private long Factorial(int n)
    {
        if (n <= 1)
            return 1;
        return n * Factorial(Dec(n));
    }
    private static int Dec(int x) => x - 1;
}
""";

    /// <summary>The /tmp/skep-recur/witness0.json oracle.</summary>
    private static readonly WitnessSpec FactorialWitness = new(
        "skep-factorial",
        [
            Case(("n", 0), ("factorial", 1)),
            Case(("n", 3), ("factorial", 6)),
            Case(("n", 5), ("factorial", 120)),
        ]);

    [Fact(Timeout = TestTimeouts.Stress)]
    public async Task HonestBrick_WhoseOnlyProblemIsAMutantThatLoopsForever_GetsAVerdict_WithTheMutantKilledByTimeout()
    {
        // At 8a6e862f this call never returns: the shift-relational-boundary mutant spins in
        // `while (n >= 0) { n /= 10; }` on the certifier's own thread and only the xunit timeout
        // ends the test (with the spinning thread left behind). Now the mutant runs in a child
        // process, times out, and the record says so — separately from the witness's own kills.
        var decision = await CreateGate().CertifyAsync(Request(DigitCountBrickSource, "Adv.Mut.DigitCountBrick", DigitsWitness));
        var record = decision.Record;

        record.TimedOutMutants.Should().NotBeEmpty(
            "the boundary shift makes the countdown nonterminating on value=0; the wall clock, not the witness, killed it (record: {0})",
            record.Reason ?? "ADMIT");
        record.TimedOutMutants.Should().Contain(
            id => id.StartsWith("shift-relational-boundary", StringComparison.Ordinal),
            "the nonterminating mutant is the relational boundary shift; timed out were [{0}]",
            string.Join(", ", record.TimedOutMutants));
        record.KilledMutants.Should().NotContain(record.TimedOutMutants,
            "a kill the clock decided must never be filed as a kill the witness decided");
        record.KilledMutants.Should().NotBeEmpty("the witness still earns its own kills on the same brick");
        record.CrashedMutants.Should().BeEmpty("nothing about a countdown loop can take a process down");
        AssertMutantAccounting(record);

        record.SchemaVersion.Should().Be(CertificationRecordData.CurrentSchemaVersion);
        var correctness = record.GatesPassed.Should().ContainSingle(g => g.Name == "correctness-witness").Subject;
        correctness.Configuration.Should().Contain($"execution={LocalProcessExecutionBackend.Identity}")
            .And.Contain("perCaseTimeoutMs=2000",
                "the budget the verdict was reached under is part of the record");

        if (decision.Admitted)
            new CertificationRecordSigner().Verify(record).Should().BeTrue("an admitted v3 record must verify as written");
        else
            decision.FailureCheck.Should().Be("mutation",
                "the only honest way this brick can fail is a surviving mutant; anything else is the harness (reason: {0})", record.Reason);
    }

    [Fact(Timeout = TestTimeouts.Stress)]
    public async Task HonestBrick_WithARecursiveHelper_GetsAVerdict_WithTheOverflowingMutantsRecordedAsCrashes()
    {
        // At 8a6e862f this test does not fail — it takes the test host down with it (exit 134,
        // "Stack overflow."): a mutant with an infinite recursion overflows the certifier's own
        // stack and no catch sees it. The oracle /tmp/skep-recur/DecBrick reproduces it through
        // tools/Ashlar.CertifyBrick as well. Now the overflow kills a child process, the certifier
        // reads the exit code, records the crash against that mutant and restarts the runner.
        var decision = await CreateGate().CertifyAsync(Request(RecursiveFactorialBrickSource, "Skep.Recur.RecurBrick", FactorialWitness));
        var record = decision.Record;

        record.CrashedMutants.Should().NotBeEmpty(
            "a mutated `x - 1` recurses without end and overflows the stack; that kills the runner, not the witness (record: {0})",
            record.Reason ?? "ADMIT");
        record.KilledMutants.Should().NotContain(record.CrashedMutants,
            "a process death is not a witness kill and must not be filed as one");
        record.KilledMutants.Should().NotBeEmpty("the witness still earns its own kills on the same brick");
        AssertMutantAccounting(record);

        if (decision.Admitted)
            new CertificationRecordSigner().Verify(record).Should().BeTrue();
        else
            decision.FailureCheck.Should().Be("mutation", record.Reason);
    }

    /// <summary>
    /// Every way a CANDIDATE can attack the process running it, and the verdict each one earns.
    /// None of them may hang the certifier or end it — with any exit code, least of all 0.
    /// </summary>
    [Theory(Timeout = TestTimeouts.Stress)]
    [InlineData("exit-0", "Environment.Exit(0);", "Crashed", "exit code 0")]
    [InlineData("fail-fast", "Environment.FailFast(\"hostile\");", "Crashed", null)]
    [InlineData("stack-overflow", "value = Depth(value);", "Crashed", null)]
    [InlineData("background-throw", "new Thread(() => throw new InvalidOperationException(\"background\")).Start(); Thread.Sleep(500);", "Crashed", null)]
    [InlineData("infinite-loop", "while (value >= 0) { value = value * 1; }", "TimedOut", "perCaseTimeoutMs")]
    [InlineData("allocate-forever", "var hoard = new List<byte[]>(); while (value >= 0) { hoard.Add(new byte[64 * 1024 * 1024]); }", "Threw|Crashed", null)]
    public async Task HostileCandidate_IsRejectedAtCorrectness_AndTheCertifierSurvives(
        string name, string body, string acceptedKinds, string? reasonFragment)
    {
        // At 8a6e862f: exit-0 ends the TEST HOST with code 0 and no failure; fail-fast,
        // stack-overflow and background-throw end it with 134; infinite-loop never returns.
        var source = HostileBrickSource(body);

        var decision = await CreateGate().CertifyAsync(Request(source, "Hostile.HostileBrick", HostileWitness));

        decision.Admitted.Should().BeFalse("{0}: a brick that attacks its host has no business being certified", name);
        decision.FailureCheck.Should().Be("correctness",
            "{0}: the attack fires on the candidate's own witness run, so the correctness leg is where it is caught (reason: {1})",
            name, decision.Record.Reason);
        var accepted = acceptedKinds.Split('|').Select(Enum.Parse<WitnessFindingKind>).ToArray();
        decision.WitnessFindings.Should().NotBeEmpty();
        decision.WitnessFindings.Select(f => f.Kind).Should().OnlyContain(kind => accepted.Contains(kind),
            "{0}: the finding must name what actually happened, not call it a throw; findings were [{1}]",
            name, string.Join(", ", decision.WitnessFindings.Select(f => $"{f.Kind}:{f.Detail}")));
        if (reasonFragment is not null)
            decision.Record.Reason.Should().Contain(reasonFragment, "{0}: the reason names the mechanism", name);
        decision.Record.TotalMutants.Should().Be(0, "{0}: a candidate that fails correctness never reaches the mutation leg", name);
    }

    [Fact]
    public void RecordSchemaV3_SignsTheWallClockKills_AndV2RecordsStillVerify()
    {
        // Without this, a record could be edited to move a timed-out id into killedMutants — teeth
        // the witness never showed — and still verify. And the v2 records already on disk (the
        // tracked samples, every consumer's sidecar) must keep verifying under their own payload.
        var signer = new CertificationRecordSigner("bounded-execution-test-key");
        var v3 = signer.SignRecord(RecordAt(CertificationRecordData.CurrentSchemaVersion) with
        {
            KilledMutants = ["mutate-int-literal:L10"],
            TimedOutMutants = ["shift-relational-boundary:L12"],
            CrashedMutants = ["swap-arithmetic-op:L20"],
        });

        signer.Verify(v3).Should().BeTrue("a freshly signed v3 record verifies");
        signer.Verify(v3 with { TimedOutMutants = [] }).Should().BeFalse("dropping a timed-out id changes the signed payload");
        signer.Verify(v3 with { CrashedMutants = [] }).Should().BeFalse("dropping a crashed id changes the signed payload");
        signer.Verify(v3 with
        {
            KilledMutants = ["mutate-int-literal:L10", "shift-relational-boundary:L12"],
            TimedOutMutants = [],
        }).Should().BeFalse("a wall-clock kill cannot be laundered into a witness kill");
        CertificationRecordSigning.BuildPayload(CertificationRecordMapper.ToData(v3))
            .Should().Contain("timedOutMutants").And.Contain("crashedMutants");

        var v2 = signer.SignRecord(RecordAt(CertificationRecordData.TrustLoopSchemaVersion));
        signer.Verify(v2).Should().BeTrue("records minted before v3 verify exactly as before");
        CertificationRecordSigning.BuildPayload(CertificationRecordMapper.ToData(v2))
            .Should().NotContain("timedOutMutants", "the v2 payload is byte-for-byte what v2 signers produced");
        signer.Verify(v2 with { SchemaVersion = CertificationRecordData.CurrentSchemaVersion })
            .Should().BeFalse("a v2 signature does not carry over to the v3 payload, so an upgrade-by-edit is refused");
    }

    [Fact]
    public void TheReplayRunner_CarriesTheMarkersAndExitCodeTheGateParses()
    {
        // The runner is compiled from this string in a child process; the certifier is compiled
        // from ExecutionRunnerMarkers. If either literal drifts, timeouts silently become witness
        // kills and load failures become kills — the vacuous-kill failure mode. Nothing else notices.
        WitnessReplayRunner.Source.Should().Contain($"\"{ExecutionRunnerMarkers.ExecutionTimeoutPrefix}\"");
        WitnessReplayRunner.Source.Should().Contain($"\"{ExecutionRunnerMarkers.UnitLoadFailurePrefix}\"");
        WitnessReplayRunner.Source.Should().Contain($"ExitTimedOut = {WitnessReplayRunner.ExitTimedOut};");
        // The crash marker is the certifier's alone: a runner that died cannot report its own death.
        WitnessReplayRunner.Source.Should().NotContain(ExecutionRunnerMarkers.RunnerCrashPrefix);
    }

    private static void AssertMutantAccounting(CertificationRecord record)
    {
        record.TotalMutants.Should().Be(
            record.KilledMutants.Count + record.TimedOutMutants.Count + record.CrashedMutants.Count + record.SurvivingMutantIds.Count,
            "every mutant is in exactly one list: killed by the witness, stopped by the clock, crashed, or survived");
        record.KilledMutants.Intersect(record.TimedOutMutants).Should().BeEmpty();
        record.KilledMutants.Intersect(record.CrashedMutants).Should().BeEmpty();
        record.TimedOutMutants.Intersect(record.CrashedMutants).Should().BeEmpty();
        record.SurvivingMutantIds.Intersect(record.TimedOutMutants.Concat(record.CrashedMutants)).Should().BeEmpty();
    }

    private static CertificationRecord RecordAt(int schemaVersion) => new()
    {
        Status = "PASS",
        Stage = "S0-S2",
        Admitted = true,
        Signed = true,
        Timestamp = new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero),
        BrickId = "bounded-execution",
        ContentHash = "hash-abc",
        EscapeRate = 0,
        TotalMutants = 3,
        SurvivingMutants = 0,
        Gate = "Ashlar.Infrastructure.Certification.CertificationGate",
        SchemaVersion = schemaVersion,
        GatesPassed = [new CertificationGatePass { Name = "mutation-gate", Version = "1", Configuration = "escapeRateThreshold=0" }],
    };

    private static readonly WitnessSpec HostileWitness = new("hostile", [Case(("value", 1), ("echo", 1))]);

    private static string HostileBrickSource(string body) => $$"""
using System.Threading;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Hostile;

public sealed class HostileBrick : Brick
{
    public HostileBrick()
    {
        Id = "hostile";
        Name = "hostile";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "attacks the process running it";
        Interface = new BrickInterface
        {
            Inputs = [ new BrickInputDefinition("value", "int", "value") ],
            Outputs = [ new BrickOutputDefinition("echo", "int", "echo") ]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var value = input.Get<int>("value");
        {{body}}
        var output = new BrickOutput();
        output.Set("echo", value);
        return Task.FromResult(output);
    }

    // Not a tail call (the `1 +` keeps the frame), so the JIT cannot turn it into a loop.
    private static int Depth(int n) => 1 + Depth(n + 1);
}
""";

    private static WitnessCase Case((string Key, object Value) input, (string Key, object Value) expected) => new(
        new Dictionary<string, object> { [input.Key] = input.Value },
        new Dictionary<string, object> { [expected.Key] = expected.Value });

    private static CertificationGate CreateGate() =>
        new(new CertificationRecordSigner(), executionLimits: TestLimits);

    /// <summary>
    /// The correctness leg replays the candidate; the mutation leg compiles SourceCode. Both come
    /// from the SAME text, and the brick instance here is loaded from bytes — so the gate has no
    /// on-disk artifact and replays the source it was given, exactly like a generated brick.
    /// </summary>
    private static CertificationRequest Request(string source, string typeName, WitnessSpec witness) => new()
    {
        Brick = CertifiedBrickCompiler.InstantiateBrick(source, typeName),
        Witness = witness,
        SourceCode = source,
        ProjectPath = CreateCleanProjectFile(),
        CompilationReferences =
        [
            typeof(DomainBrick).Assembly.Location,
            typeof(BrickInput).Assembly.Location,
        ],
        BrickTypeName = typeName,
    };

    private static string CreateCleanProjectFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ashlar-cert-bounded-{Guid.NewGuid():N}.csproj");
        File.WriteAllText(path, """
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Ashlar.Brick.Contracts" Version="0.1.0" />
    <PackageReference Include="Ashlar.Authoring" Version="0.1.0" />
  </ItemGroup>
</Project>
""");
        return path;
    }
}
