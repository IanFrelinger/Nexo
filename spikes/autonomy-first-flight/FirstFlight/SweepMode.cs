using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.BackgroundAgents.Autonomy;
using Nexo.BackgroundAgents.Objectives;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Autonomy;
using Nexo.Infrastructure.Certification.HotSwap;
using Nexo.Infrastructure.Certification.Sdk.Extensions;

namespace Nexo.Spikes.FirstFlight;

/// <summary>
/// Drives ONE sweep of the standing loop against the real objective store — the path that
/// had never run end to end: store → sweep → witness load → proposal → certification →
/// outcome. Everything upstream of this was proven by hand-constructed candidates inside
/// a spike; this is the first time an objective FILE drives the loop.
///
/// <para>Runs with hold admission on, so nothing can swap whatever the verdict.</para>
/// </summary>
public static class SweepMode
{
    /// <summary>Runs one sweep and reports. Returns a process exit code.</summary>
    public static async Task<int> RunAsync(string objectivesRoot, string sessionImage)
    {
        Console.WriteLine("== autonomy loop: one sweep of the objective store ==");
        Console.WriteLine($"store : {objectivesRoot}");
        Console.WriteLine($"image : {sessionImage}");

        var store = new ObjectiveStore(objectivesRoot);
        var pending = store.List(ObjectiveStatus.Pending);
        Console.WriteLine($"pending objectives: {pending.Count}");
        foreach (var o in pending)
            Console.WriteLine($"  - {o.Id}  (source={o.Source}, priority={o.Priority})");

        if (pending.Count == 0)
        {
            Console.WriteLine("SWEEP: nothing to do — no pending objectives in the store");
            return 1;
        }

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Information));
        services.AddCertificationGate();
        services.AddNexoAutonomy(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nexo:Autonomy:Enabled"] = "true",
                ["Nexo:Autonomy:UseSandboxSessions"] = "true",
                ["Nexo:Autonomy:BuildCandidateInSession"] = "true",
                ["Nexo:Autonomy:ExecuteCandidateInSession"] = "true",
                // The whole point of the first run: certify everything, admit nothing.
                ["Nexo:Autonomy:HoldAdmission"] = "true",
                ["Nexo:Autonomy:SessionImage"] = sessionImage,
                ["Nexo:Autonomy:CadenceFloorSeconds"] = "0",
            })
            .Build());

        await using var provider = services.BuildServiceProvider();
        var harness = provider.GetRequiredService<AutonomousIterationHarness>();

        var settings = new AutonomyLoopSettings
        {
            IntervalSeconds = 0, // driven manually here, not on a timer
            HoldAdmission = true,
            SessionImage = sessionImage,
            MaxObjectivesPerSweep = 5,
            CompilationReferences = new[]
            {
                typeof(DomainBrick).Assembly.Location,
                typeof(BrickInput).Assembly.Location,
                // The candidate delegates to the physical-atom codec, so its assembly
                // has to travel into the session with the rest of the references.
                typeof(Nexo.Certification.Physical.Tagging.PhysicalAtomQrTagCodec).Assembly.Location,
            },
        };

        // Campaign mode: a LIVE proposer inside the loop, so the loop's own repair channel
        // (policy-projected feedback, bounded attempts) is what runs — not a recorded proposal.
        // Every proposal, and the exact projected feedback the model was handed for a repair,
        // is recorded to the campaign directory: that is the evidence ledger's raw material.
        IProposalSource? proposals = null;
        var proposerKind = Environment.GetEnvironmentVariable("NEXO_SWEEP_PROPOSER");
        if (string.Equals(proposerKind, "ollama", StringComparison.OrdinalIgnoreCase))
        {
            var options = new OllamaProposalOptions
            {
                BaseUrl = Environment.GetEnvironmentVariable("NEXO_OLLAMA_BASE_URL") ?? "http://host.docker.internal:11434",
                Model = Environment.GetEnvironmentVariable("NEXO_OLLAMA_MODEL") ?? "codellama:7b",
            };
            // Operator preamble (house rules — here, the brick API a small model does not know).
            // Data the proposer is handed, never a witness; the same knob a deployment would set.
            var preamblePath = Environment.GetEnvironmentVariable("NEXO_OLLAMA_SYSTEM_PREAMBLE_FILE");
            if (!string.IsNullOrWhiteSpace(preamblePath) && File.Exists(preamblePath))
            {
                options.SystemPreamble = File.ReadAllText(preamblePath).Trim();
                Console.WriteLine($"preamble: {preamblePath} ({options.SystemPreamble.Length} chars)");
            }
            var campaignDir = Environment.GetEnvironmentVariable("NEXO_CAMPAIGN_DIR")
                ?? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(objectivesRoot))!, "campaign");
            Directory.CreateDirectory(campaignDir);
            var live = new OllamaProposalSource(
                new HttpClient(),
                options,
                provider.GetRequiredService<ILogger<OllamaProposalSource>>());
            proposals = new RecordingProposalSource(live, campaignDir);
            Console.WriteLine($"proposer: LIVE ollama {options.Model} @ {options.BaseUrl}  (recording to {campaignDir})");
        }
        else
        {
            Console.WriteLine("proposer: recorded proposals beside each objective (set NEXO_SWEEP_PROPOSER=ollama for live)");
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("NEXO_SWEEP_MAX_OBJECTIVES"), out var maxObjectives) && maxObjectives > 0)
            settings.MaxObjectivesPerSweep = maxObjectives;

        var loop = new AutonomyLoopService(
            store,
            harness,
            settings,
            provider.GetRequiredService<ILogger<AutonomyLoopService>>(),
            proposals);

        var started = DateTimeOffset.UtcNow;
        var attempted = await loop.SweepAsync();
        var elapsed = DateTimeOffset.UtcNow - started;

        Console.WriteLine();
        Console.WriteLine($"SWEEP: attempted {attempted} objective(s) in {elapsed.TotalSeconds:F1}s");

        // Attempting zero is a real answer, not a crash: it means every pending objective
        // was ineligible (no witness, or no proposal beside it).
        if (attempted == 0)
        {
            Console.WriteLine("SWEEP: no objective was eligible — check for a witness and proposal beside each one");
            return 1;
        }

        Console.WriteLine("SWEEP: complete (see the iteration outcome logged above)");
        return 0;
    }

    /// <summary>
    /// Records what a proposer was asked and what it answered, per objective and attempt, as
    /// <c>{campaignDir}/{objectiveId}.attempt{N}.json</c>. The recording is downstream of the
    /// policy projection: the <c>feedback</c> field is exactly what the model saw, so a
    /// reviewer can check that no witness value reached it.
    /// </summary>
    private sealed class RecordingProposalSource : IProposalSource
    {
        private static readonly System.Text.Json.JsonSerializerOptions Json = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        };

        private readonly IProposalSource _inner;
        private readonly string _dir;

        public RecordingProposalSource(IProposalSource inner, string dir)
        {
            _inner = inner;
            _dir = dir;
        }

        public async Task<ProposedSource?> ProposeAsync(ProposalRequest request, CancellationToken cancellationToken = default)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            ProposedSource? proposed = null;
            string? error = null;
            try
            {
                proposed = await _inner.ProposeAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name + ": " + ex.Message;
                throw;
            }
            finally
            {
                sw.Stop();
                var attempt = request.Repair?.Attempt ?? 0;
                var record = new
                {
                    objectiveId = request.ObjectiveId,
                    attempt,
                    mode = request.Repair is null ? "initial" : $"repair{attempt}",
                    proposedAt = DateTimeOffset.UtcNow,
                    durationSeconds = Math.Round(sw.Elapsed.TotalSeconds, 1),
                    feedback = request.Repair?.Feedback,
                    signature = proposed?.ProposerSignature,
                    typeName = proposed?.TypeName,
                    sourceCode = proposed?.SourceCode,
                    error,
                };
                var path = Path.Combine(_dir, $"{request.ObjectiveId}.attempt{attempt}.json");
                try
                {
                    File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(record, Json));
                    Console.WriteLine($"  recorded {Path.GetFileName(path)} ({record.durationSeconds}s{(proposed is null ? ", NO PROPOSAL" : string.Empty)})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  (could not record {path}: {ex.Message})");
                }
            }

            return proposed;
        }
    }
}
