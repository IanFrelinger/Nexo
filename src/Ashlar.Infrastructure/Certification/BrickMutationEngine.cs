using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Infrastructure.Testing.CodeAnalysis;

namespace Ashlar.Infrastructure.Certification;

/// <summary>Generates and compiles AST mutants for brick certification mutation testing.</summary>
internal sealed class BrickMutationEngine
{
    /// <summary>
    /// Process-wide gate ensuring only one mutant load context exists at a time.
    /// </summary>
    /// <remarks>
    /// Serialising within a single <see cref="RunAsync"/> loop is not sufficient. Callers
    /// run concurrently — xunit executes collections in parallel, and certification can be
    /// driven from several places at once — so without this gate two threads create,
    /// unload and collect their own collectible contexts simultaneously. Finalizing
    /// overlapping <c>LoaderAllocator</c>s is what crashes the runtime
    /// (<c>LoaderAllocatorScout.Finalize</c> / <c>0x80131506</c>), and it takes the whole
    /// process down rather than failing the caller. Mutation runs are dominated by Roslyn
    /// compilation anyway, so making them mutually exclusive costs little.
    /// The gate is shared with the hot-swap host
    /// (<see cref="HotSwap.CollectibleLoadContextGate"/>): a mutant context and a brick
    /// generation must never be torn down concurrently either.
    /// </remarks>
    private static SemaphoreSlim MutantContextGate => HotSwap.CollectibleLoadContextGate.Instance;

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
    /// Run asynchronously. With an execution <paramref name="backend"/>, mutants still
    /// COMPILE in this process (Roslyn is trusted; the mutant never executes here) but
    /// every mutant EXECUTION routes through the backend in one batch, and the kill
    /// verdicts are judged here from raw observations. Backend infrastructure failures
    /// throw — a mutant must never count as killed because the backend fell over
    /// (vacuous kills are the failure mode this gate exists to prevent).
    /// </summary>
    public async Task<MutationTestResult> RunAsync(
        string sourceCode,
        string brickTypeName,
        WitnessSpec witness,
        IReadOnlyList<string> compilationReferences,
        CancellationToken cancellationToken,
        ICandidateExecutionBackend? backend = null,
        AnalyzerFenceGate? analyzerFence = null,
        BrickCompileOptions? compileOptions = null)
    {
        var survivors = new List<string>();
        var killed = new List<string>();
        // Parsed, bound and — below — compiled under the BUILD's options, so the mutants are
        // mutants of the program that shipped, not of a default parse of the same bytes.
        var mutations = AstMutationCatalog.CollectMutations(sourceCode, compilationReferences, compileOptions);

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

            if (backend is not null)
            {
                var image = await CompileMutantAsync(
                    mutatedSource, compilationReferences, compileOptions, cancellationToken).ConfigureAwait(false);
                if (image is null)
                    killed.Add(mutation.Id); // Non-compiling mutant: dead on arrival, as in-proc.
                else
                {
                    pendingUnits.Add(new CandidateExecutionUnit(mutation.Id, image, brickTypeName));
                    pendingSources[mutation.Id] = mutatedSource;
                }
                continue;
            }

            bool witnessPassed = await RunMutantInIsolationAsync(
                mutatedSource,
                brickTypeName,
                witness,
                compilationReferences,
                compileOptions,
                cancellationToken).ConfigureAwait(false);

            if (!witnessPassed)
            {
                killed.Add(mutation.Id);
                continue;
            }

            if (await FenceWouldRejectAsync(mutatedSource, analyzerFence, compilationReferences, compileOptions, cancellationToken).ConfigureAwait(false))
                killed.Add(mutation.Id); // Analyzer-dead: could never certify, so not an escape.
            else
                survivors.Add(mutation.Id);
        }

        if (backend is not null && pendingUnits.Count > 0)
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
                    killed.Add(unit.UnitId);
                    continue;
                }

                if (await FenceWouldRejectAsync(pendingSources[unit.UnitId], analyzerFence, compilationReferences, compileOptions, cancellationToken).ConfigureAwait(false))
                    killed.Add(unit.UnitId); // Analyzer-dead, as in-proc.
                else
                    survivors.Add(unit.UnitId);
            }
        }

        var total = survivors.Count + killed.Count;
        var escapeRate = total == 0 ? 0d : (double)survivors.Count / total;
        var survivorSites = survivors
            .Where(siteById.ContainsKey)
            .Select(id => new MutationSurvivor(id, siteById[id]))
            .ToArray();

        return new MutationTestResult(total, survivors, killed, escapeRate, survivorSites);
    }

    /// <summary>
    /// Refuses a backend unit that never actually executed.
    /// </summary>
    /// <remarks>
    /// The runner reports a unit it could not LOAD as a thrown observation on every case, which
    /// is byte-identical in shape to a mutant that throws on every case — and the judge reads
    /// both as killed. One of those is a witness doing its job; the other is a mutation leg with
    /// no harness behind it, and counting it inflates the kill count of a certificate the gate is
    /// about to sign. The runner marks the difference explicitly
    /// (<see cref="HotSwap.ExecutionRunnerMarkers.UnitLoadFailurePrefix"/>), so honour it.
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

        var loadFailure = observations.FirstOrDefault(o =>
            o.Threw
            && o.Error is not null
            && o.Error.StartsWith(HotSwap.ExecutionRunnerMarkers.UnitLoadFailurePrefix, StringComparison.Ordinal));
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
        BrickCompileOptions? compileOptions,
        CancellationToken cancellationToken)
    {
        if (analyzerFence is null)
            return false;

        try
        {
            var outcome = await analyzerFence.EvaluateAsync(
                mutatedSource, compilationReferences, compileOptions: compileOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
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
        BrickCompileOptions? compileOptions,
        CancellationToken cancellationToken)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ashlar-cert-mut", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var assemblyName = $"MutantBrick_{Guid.NewGuid():N}";
            var outputPath = Path.Combine(tempDir, $"{assemblyName}.dll");
            var compiler = new RoslynCodeAnalysisService(NullLogger<RoslynCodeAnalysisService>.Instance);
            var compile = await BrickCompilation.CompileAsync(
                compiler,
                WrapWithGlobalUsings(mutatedSource),
                assemblyName,
                outputPath,
                compilationReferences,
                compileOptions,
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

    /// <summary>
    /// Compiles one mutant, runs the witness against it, and releases the load context
    /// before returning.
    /// </summary>
    /// <remarks>
    /// The load context must be fully released before the next mutant is compiled.
    /// Previously each mutant's context was unloaded while the caller still held the
    /// mutant instance and assembly, and the loop immediately created the next context,
    /// so several collectible contexts were mid-unload at once. Finalizing those
    /// overlapping <c>LoaderAllocator</c>s crashed the runtime outright
    /// (<c>LoaderAllocatorScout.Finalize</c> / <c>0x80131506</c>), taking the whole
    /// process with it. Unloading is serialised here so at most one context is ever
    /// being collected.
    /// </remarks>
    private static async Task<bool> RunMutantInIsolationAsync(
        string sourceCode,
        string brickTypeName,
        WitnessSpec witness,
        IReadOnlyList<string> compilationReferences,
        BrickCompileOptions? compileOptions,
        CancellationToken cancellationToken)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ashlar-cert-mut", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        // Held across load, execution, unload AND collection, so no second context can
        // exist while this one is being torn down.
        await MutantContextGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            (bool passed, WeakReference contextRef) = await ExecuteMutantAsync(
                sourceCode,
                brickTypeName,
                witness,
                compilationReferences,
                compileOptions,
                tempDir,
                cancellationToken).ConfigureAwait(false);

            WaitForContextRelease(contextRef);
            return passed;
        }
        finally
        {
            MutantContextGate.Release();

            // Deleted only after the context is released. While the assembly is loaded
            // the file is mapped and the delete silently fails, which is why these temp
            // directories used to accumulate for the life of the process.
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }

    /// <summary>
    /// Runs a single mutant inside its own collectible context and returns only a bool
    /// plus a weak handle to that context.
    /// </summary>
    /// <remarks>
    /// Nothing that lives in the mutant's context — instance, assembly, or types — may
    /// escape this method, or the context can never be collected. NoInlining keeps the
    /// locals from being hoisted into the caller's frame and outliving the unload.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(bool Passed, WeakReference ContextRef)> ExecuteMutantAsync(
        string sourceCode,
        string brickTypeName,
        WitnessSpec witness,
        IReadOnlyList<string> compilationReferences,
        BrickCompileOptions? compileOptions,
        string tempDir,
        CancellationToken cancellationToken)
    {
        var assemblyName = $"MutantBrick_{Guid.NewGuid():N}";
        var outputPath = Path.Combine(tempDir, $"{assemblyName}.dll");
        MutantAssemblyLoadContext? loadContext = null;

        try
        {
            var compiler = new RoslynCodeAnalysisService(NullLogger<RoslynCodeAnalysisService>.Instance);
            var compile = await BrickCompilation.CompileAsync(
                compiler,
                WrapWithGlobalUsings(sourceCode),
                assemblyName,
                outputPath,
                compilationReferences,
                compileOptions,
                cancellationToken).ConfigureAwait(false);

            if (!compile.Success || string.IsNullOrWhiteSpace(compile.AssemblyPath) || !File.Exists(compile.AssemblyPath))
                return (false, new WeakReference(null));

            loadContext = new MutantAssemblyLoadContext(assemblyName);
            var assembly = loadContext.LoadFromAssemblyPath(compile.AssemblyPath);
            var type = assembly.GetType(brickTypeName) ?? assembly.GetTypes().FirstOrDefault(t => t.IsClass && !t.IsAbstract && t.Name != "CertAuditContext");
            if (type is null)
                return (false, new WeakReference(loadContext));

            var instance = Activator.CreateInstance(type);
            if (instance is null)
                return (false, new WeakReference(loadContext));

            // Awaited to completion here, inside the frame that owns the context. The
            // witness invokes the mutant's ExecuteAsync reflectively, so returning
            // before it settles would leave continuations running against a context
            // that is already unloading.
            bool passed = await MutantWitnessExecutor.RunWitnessAsync(
                instance,
                assembly,
                witness,
                cancellationToken).ConfigureAwait(false);

            return (passed, new WeakReference(loadContext));
        }
        catch (CertificationHarnessException)
        {
            // NOT a kill. A harness that cannot drive the mutant has observed nothing about the
            // witness, and folding that into (false, ...) is exactly how the leg came to report a
            // clean sweep it never ran. Propagates as an infrastructure fault, like a backend one.
            throw;
        }
        catch
        {
            return (false, new WeakReference(loadContext));
        }
        finally
        {
            // Unconditional: the old code only unloaded on the success path, so a
            // throwing witness or a failed Activator.CreateInstance leaked the context.
            loadContext?.Unload();
        }
    }

    /// <summary>
    /// Drives collection until the load context is actually gone.
    /// </summary>
    /// <remarks>
    /// <see cref="AssemblyLoadContext.Unload"/> only requests unloading; the allocator
    /// is freed once the last reference drops, which needs a collection. Draining here
    /// keeps the number of contexts awaiting finalization at one.
    /// </remarks>
    private static void WaitForContextRelease(WeakReference contextRef)
    {
        // Cheap check first. Once the owning frame has exited, the context is usually
        // already unreachable and a background collection will reclaim it without any
        // help — in which case forcing a blocking full GC per mutant buys nothing and
        // costs a great deal across a whole certification run.
        if (!contextRef.IsAlive)
            return;

        // Still reachable, so collection has to be driven. This is the case the gate
        // exists for: the next mutant must not be loaded while this allocator is
        // waiting to be finalized.
        const int maxAttempts = 10;
        for (int attempt = 0; attempt < maxAttempts && contextRef.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    // The wrap lives in CandidateSourceWrapper so the analyzer gate compiles byte-identical
    // candidate text (spec A1.2: analyzer and compiler must see the same bytes).
    private static string WrapWithGlobalUsings(string sourceCode)
        => CandidateSourceWrapper.Wrap(sourceCode);
}
