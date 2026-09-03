using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Infrastructure.Certification.HotSwap;
using Ashlar.Infrastructure.Testing.CodeAnalysis;

namespace Ashlar.Infrastructure.Certification;

/// <summary>Generates and compiles AST mutants for brick certification mutation testing.</summary>
/// <remarks>
/// <para>Mutants COMPILE in this process — Roslyn is trusted and a mutant that is merely
/// compiled has not run — and EXECUTE somewhere else, always: through the request's execution
/// backend when it names one (the attested session), otherwise through
/// <see cref="LocalProcessExecutionBackend"/>, a bounded child process on this machine. The
/// in-process path this engine once had loaded each mutant into a collectible load context and
/// invoked it reflectively on the certifier's own threads. It could not bound that call: a
/// <c>shift-relational-boundary</c> mutant that turned <c>while (n &gt; 0)</c> into
/// <c>while (n &gt;= 0)</c> hung certification of an honest brick forever, and a mutated literal
/// in an honest recursive helper overflowed the stack, which no <c>catch</c> sees and which took
/// the whole process down without a verdict. There is no in-process execution left here.</para>
///
/// <para>Every mutant EXECUTION routes through the backend in one batch, and the kill verdicts
/// are judged here from raw observations. Backend infrastructure failures throw — a mutant must
/// never count as killed because the backend fell over (vacuous kills are the failure mode this
/// gate exists to prevent). Kills the WALL CLOCK or a PROCESS DEATH decided are filed separately
/// from kills the witness decided (see <see cref="MutationTestResult"/>).</para>
/// </remarks>
internal sealed class BrickMutationEngine
{
    /// <summary>Gets mutation strategy names.</summary>
    public IReadOnlyList<string> GetMutationStrategyNames() =>
    [
        "flip-binary-op",
        "negate-condition",
        "mutate-int-literal",
        "mutate-string-literal",
        "remove-statement",
        "swap-logical-op",
        "degrade-coalesce-assign",
        "swap-arithmetic-op",
        "swap-arithmetic-assign",
        "shift-relational-boundary",
        "swap-unary-op",
        "remove-logical-not"
    ];

    /// <summary>
    /// Run asynchronously. Mutants compile here; every mutant EXECUTION goes through
    /// <paramref name="backend"/> — the caller's, or a mutants-only local child process under
    /// <see cref="CandidateExecutionLimits.Default"/> when none is given.
    /// </summary>
    public async Task<MutationTestResult> RunAsync(
        string sourceCode,
        string brickTypeName,
        WitnessSpec witness,
        IReadOnlyList<string> compilationReferences,
        CancellationToken cancellationToken,
        ICandidateExecutionBackend? backend = null,
        AnalyzerFenceGate? analyzerFence = null)
    {
        using var localBackend = backend is null
            ? LocalProcessExecutionBackend.CreateForMutantsOnly(compilationReferences, CandidateExecutionLimits.Default)
            : null;
        backend ??= localBackend!;

        var survivors = new List<string>();
        var killed = new List<string>();
        var timedOut = new List<string>();
        var crashed = new List<string>();
        var mutations = AstMutationCatalog.CollectMutations(sourceCode, compilationReferences);

        // Survivors are reported by id (kind, line, disambiguated with #2/#3 on collision so the
        // signed ledger is unambiguous). Keep each mutation's site so the rejection can say WHAT
        // changed — the difference between "the witness is weak here" and "this mutant is
        // equivalent and no witness could ever kill it".
        var siteById = new Dictionary<string, MutationSite>(StringComparer.Ordinal);
        foreach (var mutation in mutations)
            siteById.TryAdd(mutation.Id, mutation.Site);
        var pendingUnits = new List<CandidateExecutionUnit>();
        var pendingSources = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var mutation in mutations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var mutatedSource = mutation.ToSource();
            if (string.Equals(mutatedSource, sourceCode, StringComparison.Ordinal))
            {
                killed.Add(mutation.Id);
                continue;
            }

            var image = await CompileMutantAsync(
                mutatedSource, compilationReferences, cancellationToken).ConfigureAwait(false);
            if (image is null)
            {
                killed.Add(mutation.Id); // Non-compiling mutant: dead on arrival.
                continue;
            }

            pendingUnits.Add(new CandidateExecutionUnit(mutation.Id, image, brickTypeName));
            pendingSources[mutation.Id] = mutatedSource;
        }

        if (pendingUnits.Count > 0)
        {
            var report = await backend.ExecuteAsync(
                new CandidateExecutionJob(pendingUnits, witness, Repeats: 1),
                cancellationToken).ConfigureAwait(false);

            foreach (var unit in pendingUnits)
            {
                var observations = report.Observations.Where(o => o.UnitId == unit.UnitId).ToArray();
                ThrowIfUnitNeverRan(unit.UnitId, observations);
                if (!WitnessRunner.JudgeMutantObservations(witness, observations))
                {
                    ClassifyDeath(unit.UnitId, observations, killed, timedOut, crashed);
                    continue;
                }

                if (await FenceWouldRejectAsync(pendingSources[unit.UnitId], analyzerFence, compilationReferences, cancellationToken).ConfigureAwait(false))
                    killed.Add(unit.UnitId); // Analyzer-dead: could never certify, so not an escape.
                else
                    survivors.Add(unit.UnitId);
            }
        }

        var total = survivors.Count + killed.Count + timedOut.Count + crashed.Count;
        var escapeRate = total == 0 ? 0d : (double)survivors.Count / total;
        var survivorSites = survivors
            .Where(siteById.ContainsKey)
            .Select(id => new MutationSurvivor(id, siteById[id]))
            .ToArray();

        return new MutationTestResult(total, survivors, killed, escapeRate, survivorSites, timedOut, crashed);
    }

    /// <summary>
    /// Files a dead mutant under the ONE list that says why it died. A crash outranks a timeout
    /// (a process that died tells us less than one that was stopped), and either outranks a
    /// witness kill: if the wall clock or a process death intervened anywhere in the unit's run,
    /// the witness's verdict over the full case set was never observed, and claiming it would
    /// put teeth on the certificate the witness did not show.
    /// </summary>
    private static void ClassifyDeath(
        string unitId,
        IReadOnlyList<CandidateCaseObservation> observations,
        List<string> killed,
        List<string> timedOut,
        List<string> crashed)
    {
        if (observations.Any(o => HasMarker(o, ExecutionRunnerMarkers.RunnerCrashPrefix)))
            crashed.Add(unitId);
        else if (observations.Any(o => HasMarker(o, ExecutionRunnerMarkers.ExecutionTimeoutPrefix)))
            timedOut.Add(unitId);
        else
            killed.Add(unitId);
    }

    private static bool HasMarker(CandidateCaseObservation observation, string prefix) =>
        observation.Threw && observation.Error is not null && observation.Error.StartsWith(prefix, StringComparison.Ordinal);

    /// <summary>
    /// Refuses a backend unit that never actually executed.
    /// </summary>
    /// <remarks>
    /// The runner reports a unit it could not LOAD as a thrown observation on every case, which
    /// is byte-identical in shape to a mutant that throws on every case — and the judge reads
    /// both as killed. One of those is a witness doing its job; the other is a mutation leg with
    /// no harness behind it, and counting it inflates the kill count of a certificate the gate is
    /// about to sign. The runner marks the difference explicitly
    /// (<see cref="ExecutionRunnerMarkers.UnitLoadFailurePrefix"/>), so honour it.
    /// </remarks>
    private static void ThrowIfUnitNeverRan(
        string unitId, IReadOnlyList<CandidateCaseObservation> observations)
    {
        if (observations.Count == 0)
        {
            throw new CertificationHarnessException(
                "Mutation harness: the execution backend returned no observation at all for mutant "
                + $"'{unitId}'. A mutant with no observation has not been judged, and scoring it either way "
                + "would put a number in a signed record that nothing measured. Fix: check the backend's "
                + "runner output for the missing unit. Refusing rather than guessing a verdict.");
        }

        var loadFailure = observations.FirstOrDefault(o => HasMarker(o, ExecutionRunnerMarkers.UnitLoadFailurePrefix));
        if (loadFailure is not null)
        {
            throw new CertificationHarnessException(
                $"Mutation harness: mutant '{unitId}' never ran — the execution backend could not load it "
                + $"({loadFailure.Error}). Its cases threw because the harness broke, not because the witness "
                + "caught anything, so counting them as kills would report a mutation leg that never happened. "
                + "Fix: the mutant image and its reference set must load in the runner — check the unit upload "
                + "and the probe directories. Refusing rather than signing an unearned escape_rate.");
        }
    }

    /// <summary>
    /// Whether the analyzer fence would reject a mutant outright. A mutant that survives
    /// the witness but could never pass certification is not an escape — it is dead on
    /// arrival at an earlier gate, exactly like a non-compiling mutant. The canonical case:
    /// mutating a declared interface key so the code now reads an undeclared input
    /// (ASHLAR0001), which no behavioural witness can observe and only the fence can name.
    /// Counting such mutants as survivors inflates the escape rate against candidates that
    /// are actually fine.
    ///
    /// <para>Runs on SURVIVORS only, so healthy candidates pay nothing. Fails toward
    /// <c>false</c> (i.e. toward reporting the survivor): a fence error must never
    /// manufacture a kill — a vacuous kill is precisely the failure mode this gate exists
    /// to prevent.</para>
    /// </summary>
    private static async Task<bool> FenceWouldRejectAsync(
        string mutatedSource,
        AnalyzerFenceGate? analyzerFence,
        IReadOnlyList<string> compilationReferences,
        CancellationToken cancellationToken)
    {
        if (analyzerFence is null)
            return false;

        try
        {
            var outcome = await analyzerFence.EvaluateAsync(
                mutatedSource, compilationReferences, cancellationToken: cancellationToken).ConfigureAwait(false);
            return !outcome.Passed;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Compiles one mutant to PE bytes for backend execution; null = did not compile.</summary>
    private static async Task<byte[]?> CompileMutantAsync(
        string mutatedSource,
        IReadOnlyList<string> compilationReferences,
        CancellationToken cancellationToken)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ashlar-cert-mut", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var assemblyName = $"MutantBrick_{Guid.NewGuid():N}";
            var outputPath = Path.Combine(tempDir, $"{assemblyName}.dll");
            var compiler = new RoslynCodeAnalysisService(NullLogger<RoslynCodeAnalysisService>.Instance);
            var compile = await compiler.CompileAsync(
                WrapWithGlobalUsings(mutatedSource),
                assemblyName,
                outputPath,
                compilationReferences,
                cancellationToken).ConfigureAwait(false);

            if (!compile.Success || string.IsNullOrWhiteSpace(compile.AssemblyPath) || !File.Exists(compile.AssemblyPath))
                return null;

            return await File.ReadAllBytesAsync(compile.AssemblyPath, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }

    // The wrap lives in CandidateSourceWrapper so the analyzer gate compiles byte-identical
    // candidate text (spec A1.2: analyzer and compiler must see the same bytes).
    private static string WrapWithGlobalUsings(string sourceCode)
        => CandidateSourceWrapper.Wrap(sourceCode);
}
