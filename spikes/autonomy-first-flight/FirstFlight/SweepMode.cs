using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.BackgroundAgents.Autonomy;
using Nexo.BackgroundAgents.Objectives;
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

        var loop = new AutonomyLoopService(
            store,
            harness,
            settings,
            provider.GetRequiredService<ILogger<AutonomyLoopService>>());

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
}
