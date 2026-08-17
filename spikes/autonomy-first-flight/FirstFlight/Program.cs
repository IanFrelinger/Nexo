// The autonomy loop's FIRST FLIGHT: one real iteration, end to end, against a live
// container engine — the acceptance step recorded as outstanding in
// docs/certification-evidence.md ("the loop has never run against a live container
// engine"). Everything before this ran against fakes or fixtures.
//
//   objective (hand-authored, Triage source) → tier classification (Tier 0)
//   → attested sandbox session (real docker run/inspect/rm; provisioning evidence
//     per the known-limitation docs — the session executes nothing)
//   → the REAL certification chain (analyzer fence + touch-set, witness, mutation,
//     determinism, dependency) → autonomous Tier-0 swap with lineage key
//   → post-swap executions clearing the watch window → digest.
//
// --dry swaps the Docker session runner for the TestKit fake: same wiring, no daemon.
// Run via run-first-flight.ps1 (container + docker.sock) — Windows Smart App Control
// blocks freshly built DLLs on the host, so host-side `dotnet run` is a lottery.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Agents.TestKit;
using Nexo.BackgroundAgents.Objectives;
using Nexo.Certification.Contracts;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Autonomy;
using Nexo.Infrastructure.Certification.HotSwap;
using Nexo.Infrastructure.Certification.Sdk.Extensions;
using Nexo.Spikes.FirstFlight;

// --sweep: drive ONE sweep of the standing loop against the real objective store.
if (args.Contains("--sweep", StringComparer.OrdinalIgnoreCase))
{
    var root = Environment.GetEnvironmentVariable("NEXO_OBJECTIVES_ROOT")
        ?? Path.Combine(Directory.GetCurrentDirectory(), ".nexo/runtime-studio/objectives");
    return await SweepMode.RunAsync(root, "mcr.microsoft.com/dotnet/sdk:9.0");
}

var dry = args.Contains("--dry", StringComparer.OrdinalIgnoreCase);
// --session-build: the P3 leg — the candidate must COMPILE inside the attested session,
// which therefore needs an SDK image rather than bare alpine.
var sessionBuild = args.Contains("--session-build", StringComparer.OrdinalIgnoreCase);
// --session-execute: the P5 leg — witness/determinism/mutation EXECUTIONS also happen
// inside the session (real runner output required, so real mode only).
var sessionExecute = args.Contains("--session-execute", StringComparer.OrdinalIgnoreCase);
// --proposed: fly the RECORDED MODEL PROPOSAL instead of the hand-authored brick. The
// proposed source is untrusted by definition, so it may only fly with full session
// containment — its in-process handle throws if anything tries to execute it here.
var proposed = args.Contains("--proposed", StringComparer.OrdinalIgnoreCase);
// --live: fly a proposal produced by a model AT FLIGHT TIME (the run script called the
// local provider and mounted the raw recording at /nexo-live). Same containment rules
// as --proposed; the loop's verdict on it — either way — is the evidence.
var live = args.Contains("--live", StringComparer.OrdinalIgnoreCase);
if (sessionExecute && !sessionBuild)
{
    Console.WriteLine("--session-execute requires --session-build (the execution leg loads the session-built assembly)");
    return 1;
}
if (sessionExecute && dry)
{
    Console.WriteLine("--session-execute needs the real daemon: the fake session cannot produce runner output");
    return 1;
}
if ((proposed || live) && !sessionExecute)
{
    Console.WriteLine("--proposed/--live require --session-execute: model-proposed code never executes in this process");
    return 1;
}
if (proposed && live)
{
    Console.WriteLine("--proposed and --live are mutually exclusive");
    return 1;
}

LiveProposal? liveProposal = null;
if (live)
{
    liveProposal = LiveProposal.Load("/nexo-live/recording.json");
    if (liveProposal is null)
        return 1; // Load printed the reason: a garbled recording never reaches the gate.
}

var sessionImage = dry ? "fake:local" : sessionBuild ? "mcr.microsoft.com/dotnet/sdk:9.0" : "alpine:3.20";
Console.WriteLine($"== autonomy first flight ({(dry ? "DRY — fake session runner" : "REAL — live docker daemon")}"
    + $"{(sessionBuild ? ", in-session build" : "")}{(sessionExecute ? " + execution" : "")}"
    + $"{(proposed ? ", MODEL-PROPOSED candidate" : "")}) ==");

// --- compose exactly the way a host would -------------------------------------------
var services = new ServiceCollection();
services.AddLogging();
var swapSink = new CollectingSwapSink();
services.AddSingleton<ICertifiedBrickSwapProvenanceSink>(swapSink);
FakeSandboxedSessionRunner? fakeSessions = null;
if (dry)
{
    fakeSessions = new FakeSandboxedSessionRunner(FakeSandboxedSessionRunner.Success("9.0.100-fake"));
    services.AddSingleton<ISandboxedSessionRunner>(fakeSessions);
}

services.AddCertificationGate();
services.AddNexoAutonomy(new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Nexo:Autonomy:Enabled"] = "true",
        ["Nexo:Autonomy:UseSandboxSessions"] = "true",
        ["Nexo:Autonomy:BuildCandidateInSession"] = sessionBuild ? "true" : "false",
        ["Nexo:Autonomy:ExecuteCandidateInSession"] = sessionExecute ? "true" : "false",
        ["Nexo:Autonomy:SessionImage"] = sessionImage,
        ["Nexo:Autonomy:CadenceFloorSeconds"] = "0",
        ["Nexo:Autonomy:WatchMinInvocations"] = "2",
    })
    .Build());

await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateOnBuild = true,
    ValidateScopes = true,
});

var harness = provider.GetRequiredService<AutonomousIterationHarness>();
var host = provider.GetRequiredService<CertifiedBrickHotSwapHost>();

// --- the objective, as a machine-sourced backlog document ----------------------------
var objective = new ObjectiveDocument
{
    Id = "first-flight-log-scanner",
    Title = "Provide a deterministic error-count brick for the flight fixture",
    Source = ObjectiveSource.Triage,
    Touch = new TouchSet
    {
        PathPrefixes = new[] { "spikes/autonomy-first-flight/generated/" },
        Namespaces = new[] { "Nexo.Spikes.FirstFlight" },
        Capabilities = new[] { "repo.fs.write" },
    },
    CreatedAt = DateTimeOffset.UtcNow,
    UpdatedAt = DateTimeOffset.UtcNow,
    Body = "First-flight fixture objective.",
};

// The ObjectiveDocument→context projection. This glue lives in the spike deliberately:
// BackgroundAgents cannot see Infrastructure's context types and Infrastructure cannot
// see ObjectiveDocument, so the HOST is where the two meet — which is what this is.
var context = new ProposalIterationContext
{
    ObjectiveId = objective.Id,
    Source = objective.Source,
    Touch = objective.Touch,
    // The proposer signature IS the provenance channel (R4.1): for model proposals —
    // recorded or live — the model identity gets hash-bound into the generation-depth input.
    Lineage = GenerationLineage.Child(
        GenerationLineage.HumanAuthored,
        live ? liveProposal!.Signature
        : proposed ? RecordedProposal.ProposerSignature
        : "sig-first-flight-proposer"),
    SessionSpec = new SandboxSpec(
        Image: sessionImage,
        // No mounts, deliberately: the session's certified role today is provisioning
        // attestation (see the known-limitation docs) — it executes nothing, so it
        // mounts nothing. Mount-path handling gets proven when build/test move
        // in-container (the next phase), not smuggled in here untested.
        Mounts: Array.Empty<Mount>(),
        Network: NetworkAccess.None,
        Command: new[] { "sleep", "600" },
        Limits: new ResourceLimits(Memory: "256m", Pids: 64, Cpus: "1"))
    {
        // The rootfs is sealed read-only; these are the only writable paths, given back as
        // ephemeral scratch, so -SessionBuild / -SessionExecute still work in-session.
        ScratchPaths = SessionScratchPaths.Default,
    },
};

var verdict = ObjectiveTierClassifier.Classify(objective.Touch!);
Console.WriteLine($"objective '{objective.Id}' source={objective.Source} tier={verdict.Tier}");

// --- the candidate ------------------------------------------------------------------
// Default: the hand-authored gate-teeth shape. --proposed: the recorded MODEL proposal —
// same objective, same independent witness (authored before the proposal and never shown
// to the proposer), different implementation. The proposed handle's ExecuteAsync throws:
// with the execution leg on, nothing may run model-proposed code in this process.
var references = new[]
{
    typeof(DomainBrick).Assembly.Location,
    typeof(BrickInput).Assembly.Location,
};
var candidate = live
    ? new ProposalCandidate
    {
        Brick = new ProposedBrickHandle(),
        SourceCode = liveProposal!.Source,
        Witness = FlightLogScannerSource.StrongWitness,
        ProjectPath = FlightLogScannerSource.WriteCleanProjectFile(),
        CompilationReferences = references,
        BrickTypeName = liveProposal.TypeName,
    }
    : proposed
    ? new ProposalCandidate
    {
        Brick = new ProposedBrickHandle(),
        SourceCode = RecordedProposal.Source,
        Witness = FlightLogScannerSource.StrongWitness,
        ProjectPath = FlightLogScannerSource.WriteCleanProjectFile(),
        CompilationReferences = references,
        BrickTypeName = RecordedProposal.TypeName,
    }
    : new ProposalCandidate
    {
        Brick = new FlightLogScannerBrick(),
        SourceCode = FlightLogScannerSource.Code,
        Witness = FlightLogScannerSource.StrongWitness,
        ProjectPath = FlightLogScannerSource.WriteCleanProjectFile(),
        CompilationReferences = references,
        BrickTypeName = typeof(FlightLogScannerBrick).FullName,
    };
if (proposed)
    Console.WriteLine($"candidate  : RECORDED MODEL PROPOSAL, proposer signature '{RecordedProposal.ProposerSignature}'");
if (live)
{
    Console.WriteLine($"candidate  : LIVE MODEL PROPOSAL ({liveProposal!.Provider}/{liveProposal.Model}, "
        + $"{liveProposal.DurationSeconds:F0}s at flight time), signature '{liveProposal.Signature}'");
    Console.WriteLine($"             type '{liveProposal.TypeName}', {liveProposal.Source.Length} chars of source");
}

// --- one iteration, four possible terminal states ------------------------------------
var started = DateTimeOffset.UtcNow;
var result = await harness.RunIterationAsync(context, candidate);
var elapsed = DateTimeOffset.UtcNow - started;

Console.WriteLine();
Console.WriteLine($"outcome    : {result.Outcome} ({elapsed.TotalSeconds:F1}s)");
Console.WriteLine($"explanation: {result.Explanation}");
if (result.Attestation is { } att)
{
    Console.WriteLine($"attestation: image={att.Image} digest={att.ImageDigest} engine={att.EngineVersion}");
    Console.WriteLine($"             memBytes={att.EffectiveMemoryBytes} pids={att.EffectivePidsLimit} nanoCpus={att.EffectiveNanoCpus}");
}
if (result.Decision is { } decision)
{
    Console.WriteLine($"certificate: signed={decision.Record.Signed} escape_rate={decision.Record.EscapeRate}");
    foreach (var input in decision.Record.Inputs)
        Console.WriteLine($"  input {input.Kind}: {input.Id} = {Shorten(input.Hash)}");
}

if (result.Outcome != IterationOutcome.AdmittedAndSwapped)
{
    Console.WriteLine("FIRST FLIGHT: FAILED — expected AdmittedAndSwapped");
    return 1;
}

// --- prove the swapped generation actually serves, and clear the watch window --------
for (var i = 0; i < 3; i++)
{
    var output = await host.ExecuteAsync(
        FlightLogScannerSource.BrickId,
        FlightLogScannerSource.SampleInput(),
        ImplementationType.Deterministic,
        new FlightExecutionContext());
    Console.WriteLine($"invocation {i + 1}: errorCount={output.Get<int>("errorCount")} first='{output.Get<string>("firstErrorMessage")}'");
}

if (dry && fakeSessions is not null && fakeSessions.ActiveSessions != 0)
{
    Console.WriteLine($"FIRST FLIGHT: FAILED — {fakeSessions.ActiveSessions} session(s) leaked");
    return 1;
}

Console.WriteLine();
Console.WriteLine(AutonomyDigest.Render(swapSink.Snapshot()));
Console.WriteLine("FIRST FLIGHT: PASS");
return 0;

static string Shorten(string? hash) =>
    string.IsNullOrEmpty(hash) ? "(none)" : hash.Length <= 24 ? hash : hash[..24] + "…";

internal sealed class FlightExecutionContext : IExecutionContext
{
    public string AgentId => "first-flight";
    public string BehaviorId => "first-flight";
    public bool IsAirGapped => true;
    public bool AuditMode => true;
    public string Provider => "deterministic";
    public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
}

internal sealed class CollectingSwapSink : ICertifiedBrickSwapProvenanceSink
{
    private readonly object _gate = new();
    private readonly List<BrickSwapProvenanceEvent> _events = new();

    public void Record(BrickSwapProvenanceEvent provenanceEvent)
    {
        lock (_gate)
        {
            _events.Add(provenanceEvent);
        }
    }

    public IReadOnlyList<BrickSwapProvenanceEvent> Snapshot()
    {
        lock (_gate)
        {
            return _events.ToList();
        }
    }
}
