using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Infrastructure.Certification.HotSwap;
using Ashlar.Infrastructure.Testing.CodeAnalysis;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Executes candidate and mutant code in a CHILD PROCESS on the certifying machine — the
/// default containment seam for the gate's execution legs when the request names no backend of
/// its own. One child per job replays the candidate (or every mutant) sequentially and streams
/// one observation per (unit, case, repeat); this side enforces the wall clock, and when the
/// child hangs or dies it records the unit that was executing as timed out or crashed, restarts
/// the child for the units that remain, and carries on.
/// </summary>
/// <remarks>
/// <para><b>Why a process and not a thread.</b> The in-process design that preceded this ran
/// author code on the certifier's own threads. A mutant that turned <c>while (n &gt; 0)</c> into
/// <c>while (n &gt;= 0)</c> hung certification forever — nothing bounded the call — and an
/// honest recursive helper whose literal was mutated overflowed the stack, which no <c>catch</c>
/// can see and which took the whole certifier down (exit 134, no verdict, no message). A thread
/// can be abandoned but not stopped, so a "timeout" on the same thread leaves a spinning core
/// behind for the life of the host; Ashlar.Infrastructure also certifies inside long-running
/// service processes, where that leak is not acceptable. A process can be killed, and a process
/// that dies takes only itself.</para>
///
/// <para><b>Honesty of the record.</b> A kill decided by the wall clock is not a kill decided by
/// the witness. Every observation this backend synthesises carries a marker
/// (<see cref="ExecutionRunnerMarkers.ExecutionTimeoutPrefix"/> /
/// <see cref="ExecutionRunnerMarkers.RunnerCrashPrefix"/>) so the mutation engine files it under
/// <c>timedOutMutants</c> or <c>crashedMutants</c> and never under <c>killedMutants</c>.</para>
///
/// <para><b>Fail-closed.</b> A runner that cannot start, cannot execute anything, or runs out of
/// the total budget throws <see cref="CertificationHarnessException"/>: nothing was observed about
/// the remaining units, so no verdict about them can be signed either way.</para>
/// </remarks>
internal sealed class LocalProcessExecutionBackend : ICandidateExecutionBackend, IDisposable
{
    /// <summary>Stable identity recorded on the certificate's gate passes.</summary>
    public const string Identity = "local-process";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Shared-framework assemblies the runner needs beyond the compiler's default set. The
    /// mutants are compiled against the running runtime's core assemblies; the runner adds JSON
    /// and assembly loading on top.
    /// </summary>
    private static readonly string[] RunnerFrameworkAssemblies =
    [
        "System.Text.Json", "System.Text.Encodings.Web", "System.Memory", "System.Collections",
        "System.Runtime.Loader", "System.Threading", "System.Threading.Tasks", "System.Runtime.InteropServices",
    ];

    private readonly string _workDir;
    private readonly string? _candidateAssemblyPath;
    private readonly string? _candidateTypeName;
    private readonly string _brickId;
    private readonly IReadOnlyList<string> _references;
    private readonly CandidateExecutionLimits _limits;
    private readonly SemaphoreSlim _runnerBuild = new(1, 1);
    private string? _runnerPath;
    private int _jobCounter;

    /// <summary>
    /// How this backend got its runner: <c>true</c> when it compiled the runner itself, <c>false</c>
    /// when it took a compiled runner from <see cref="RunnerCompileCache"/>, <c>null</c> before
    /// the runner was needed. Observability for the cache's tests; the record does not carry it.
    /// </summary>
    internal bool? RunnerCompiledFresh { get; private set; }

    private LocalProcessExecutionBackend(
        string workDir,
        string? candidateAssemblyPath,
        string? candidateTypeName,
        string brickId,
        IReadOnlyList<string> references,
        CandidateExecutionLimits limits)
    {
        _workDir = workDir;
        _candidateAssemblyPath = candidateAssemblyPath;
        _candidateTypeName = candidateTypeName;
        _brickId = brickId;
        _references = references;
        _limits = limits;
    }

    /// <summary>
    /// A backend with no candidate: it can replay mutant images (units carrying assembly bytes)
    /// and refuses a candidate unit, for callers that drive the mutation engine on its own.
    /// </summary>
    public static LocalProcessExecutionBackend CreateForMutantsOnly(
        IReadOnlyList<string> references, CandidateExecutionLimits limits)
    {
        ArgumentNullException.ThrowIfNull(references);
        ArgumentNullException.ThrowIfNull(limits);
        limits.Validate();

        var workDir = Path.Combine(Path.GetTempPath(), "ashlar-cert-exec", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        return new LocalProcessExecutionBackend(workDir, null, null, "mutant", references, limits);
    }

    /// <summary>
    /// Creates the backend for one certification: resolves the candidate artifact the child will
    /// replay and reserves a scratch directory that <see cref="Dispose"/> removes.
    /// </summary>
    /// <remarks>
    /// The candidate is the request's <see cref="CertificationRequest.Brick"/> — the same object
    /// the in-process leg used to execute — replayed from its own on-disk assembly when it has one
    /// (the loader's built assembly, a hot-swap generation). A brick loaded from bytes has no such
    /// artifact, so its <see cref="CertificationRequest.SourceCode"/> is compiled exactly as the
    /// mutation leg compiles its mutants; the analyzer fence has already compiled that text once
    /// by the time this runs, so a failure here is the harness's and is refused as such.
    /// </remarks>
    public static async Task<LocalProcessExecutionBackend> CreateAsync(
        CertificationRequest request,
        string brickTypeName,
        CandidateExecutionLimits limits,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(limits);
        limits.Validate();

        var workDir = Path.Combine(Path.GetTempPath(), "ashlar-cert-exec", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            var (assemblyPath, typeName) = await ResolveCandidateAsync(request, brickTypeName, workDir, cancellationToken)
                .ConfigureAwait(false);
            return new LocalProcessExecutionBackend(
                workDir, assemblyPath, typeName, request.Brick.Id, request.CompilationReferences, limits);
        }
        catch
        {
            TryDeleteDirectory(workDir);
            throw;
        }
    }

    /// <inheritdoc />
    public string Describe() => Identity;

    /// <inheritdoc />
    public async Task<CandidateExecutionReport> ExecuteAsync(
        CandidateExecutionJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        var runnerPath = await EnsureRunnerAsync(cancellationToken).ConfigureAwait(false);

        var jobDir = Path.Combine(_workDir, $"job-{Interlocked.Increment(ref _jobCounter)}");
        Directory.CreateDirectory(jobDir);

        var units = new List<RunnerUnit>(job.Units.Count);
        var index = 0;
        foreach (var unit in job.Units)
        {
            if (unit.Assembly is null)
            {
                if (_candidateAssemblyPath is null || _candidateTypeName is null)
                {
                    throw new CertificationHarnessException(
                        $"Execution harness: unit '{unit.UnitId}' names the built candidate, but this backend was created for "
                        + "mutant images only and holds no candidate artifact. Fix: create the backend from the certification "
                        + "request (CreateAsync). Refusing rather than replaying nothing and calling it a verdict.");
                }

                units.Add(new RunnerUnit(unit.UnitId, _candidateAssemblyPath, _candidateTypeName, _brickId));
                continue;
            }

            var path = Path.Combine(jobDir, $"unit-{index++}.dll");
            await File.WriteAllBytesAsync(path, unit.Assembly, cancellationToken).ConfigureAwait(false);
            units.Add(new RunnerUnit(unit.UnitId, path, unit.TypeName, _brickId));
        }

        var probeDirs = ProbeDirectories(units);
        var cases = job.Witness.Cases.Select(c => new RunnerCase(EncodeAll(c.Input))).ToList();
        var slotsPerUnit = cases.Count * job.Repeats;
        var observations = units.ToDictionary(
            u => u.UnitId,
            _ => new Dictionary<(int Case, int Repeat), CandidateCaseObservation>(),
            StringComparer.Ordinal);

        var totalClock = Stopwatch.StartNew();
        var remaining = new List<RunnerUnit>(units);
        var childIndex = 0;
        while (remaining.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var budgetLeft = _limits.TotalTimeout - totalClock.Elapsed;
            if (budgetLeft <= TimeSpan.Zero)
                throw TotalBudgetExceeded(remaining.Count);

            var nonce = Guid.NewGuid().ToString("N");
            var jobPath = Path.Combine(jobDir, $"job-{++childIndex}.json");
            var payload = new RunnerJob(
                nonce, probeDirs, remaining, cases, job.Repeats,
                (int)Math.Min(int.MaxValue, _limits.PerCaseTimeout.TotalMilliseconds),
                (int)Math.Min(int.MaxValue, _limits.PerUnitTimeout.TotalMilliseconds));
            await File.WriteAllTextAsync(jobPath, JsonSerializer.Serialize(payload, JsonOptions), cancellationToken)
                .ConfigureAwait(false);

            var run = await RunChildAsync(runnerPath, jobPath, nonce, observations, budgetLeft, remaining.Count, cancellationToken)
                .ConfigureAwait(false);

            if (!run.SawReady)
            {
                throw new CertificationHarnessException(
                    $"Execution harness: the witness replay runner exited (code {run.ExitCode}) before it reported ready, so "
                    + "nothing was executed and no unit can be scored. Refusing rather than signing a verdict over units "
                    + $"that never ran. Runner stderr: {run.StderrTail}");
            }

            // Attribute the runner's fate to the unit it was executing when that fate arrived.
            if (run.LastStartedUnitId is { } started && observations.TryGetValue(started, out var slots))
            {
                if (run.KilledForNoProgress)
                {
                    var deadlineSeconds = (long)(_limits.PerCaseTimeout + _limits.ProgressGrace).TotalSeconds;
                    var perCaseMs = (long)_limits.PerCaseTimeout.TotalMilliseconds;
                    var graceMs = (long)_limits.ProgressGrace.TotalMilliseconds;
                    FillMissing(slots, started, cases.Count, job.Repeats, string.Create(CultureInfo.InvariantCulture,
                        $"{ExecutionRunnerMarkers.ExecutionTimeoutPrefix}: the runner made no progress for {deadlineSeconds}s while executing this unit and was killed by the certifier (perCaseTimeoutMs={perCaseMs} + progressGraceMs={graceMs})"));
                }
                else if (run.ExitCode == WitnessReplayRunner.ExitTimedOut)
                {
                    // The runner reported the timeout itself and ended to shed the spinning thread;
                    // its slots are already marked. Belt and braces for a runner that died mid-report.
                    FillMissing(slots, started, cases.Count, job.Repeats,
                        $"{ExecutionRunnerMarkers.ExecutionTimeoutPrefix}: the runner ended after a timeout before reporting this slot");
                }
                else if (run.ExitCode != 0 || slots.Count < slotsPerUnit)
                {
                    // The process died while — or right after — executing this unit: stack overflow,
                    // Environment.Exit, FailFast, an unhandled background-thread exception, an OOM
                    // abort. Whatever the unit reported before dying is not evidence of anything it
                    // would do in a host that survived, so EVERY slot becomes the crash.
                    var detail = run.StderrTail.Length > 0 ? $": {run.StderrTail}" : string.Empty;
                    var crash = string.Create(CultureInfo.InvariantCulture,
                        $"{ExecutionRunnerMarkers.RunnerCrashPrefix} (exit code {run.ExitCode}) while executing this unit{detail}");
                    for (var caseIndex = 0; caseIndex < cases.Count; caseIndex++)
                    {
                        for (var repeat = 0; repeat < job.Repeats; repeat++)
                            slots[(caseIndex, repeat)] = new CandidateCaseObservation(started, caseIndex, repeat, true, crash, null, null);
                    }
                }
            }

            var before = remaining.Count;
            while (remaining.Count > 0 && observations[remaining[0].UnitId].Count >= slotsPerUnit)
                remaining.RemoveAt(0);

            if (remaining.Count == before)
            {
                throw new CertificationHarnessException(
                    $"Execution harness: the witness replay runner exited (code {run.ExitCode}) without completing a single "
                    + $"unit; {remaining.Count} unit(s) remain unexecuted and nothing about them was observed. Refusing "
                    + $"rather than scoring them. Runner stderr: {run.StderrTail}");
            }
        }

        var report = new List<CandidateCaseObservation>(units.Count * slotsPerUnit);
        foreach (var unit in units)
        {
            var slots = observations[unit.UnitId];
            for (var caseIndex = 0; caseIndex < cases.Count; caseIndex++)
            {
                for (var repeat = 0; repeat < job.Repeats; repeat++)
                {
                    if (slots.TryGetValue((caseIndex, repeat), out var observation))
                        report.Add(observation);
                }
            }
        }

        return new CandidateExecutionReport(report);
    }

    /// <summary>Removes the scratch directory; nothing in it outlives the certification.</summary>
    public void Dispose()
    {
        _runnerBuild.Dispose();
        TryDeleteDirectory(_workDir);
    }

    private static void FillMissing(
        Dictionary<(int Case, int Repeat), CandidateCaseObservation> slots, string unitId, int cases, int repeats, string error)
    {
        for (var caseIndex = 0; caseIndex < cases; caseIndex++)
        {
            for (var repeat = 0; repeat < repeats; repeat++)
                slots.TryAdd((caseIndex, repeat), new CandidateCaseObservation(unitId, caseIndex, repeat, true, error, null, null));
        }
    }

    private CertificationHarnessException TotalBudgetExceeded(int unexecuted) => new(
        $"Execution harness: the certification's total execution budget ({_limits.TotalTimeout}) was exhausted with "
        + $"{unexecuted} unit(s) still unexecuted. Nothing about those units was observed, so no verdict can be signed "
        + "over them. Fix: raise CandidateExecutionLimits.TotalTimeout on the gate, or reduce the witness. Refusing "
        + "rather than scoring mutants that never ran.");

    private async Task<ChildRun> RunChildAsync(
        string runnerPath,
        string jobPath,
        string nonce,
        Dictionary<string, Dictionary<(int Case, int Repeat), CandidateCaseObservation>> observations,
        TimeSpan budgetLeft,
        int unexecuted,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = LocateDotnetHost(),
            WorkingDirectory = Path.GetDirectoryName(runnerPath)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        psi.ArgumentList.Add(runnerPath);
        psi.ArgumentList.Add(jobPath);
        // The runner gets the allowlisted environment (ChildProcessEnvironment), not the certifier's:
        // the candidate it executes must not be able to read the key that signs its own certificate,
        // and a startup hook — code the runtime injects into every process it starts — is not on the
        // list, so the runner executes exactly what the job names and nothing the certifier's
        // environment adds. The bounds below are set after, on top of the allowlist.
        ChildProcessEnvironment.Apply(psi);
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        psi.Environment["DOTNET_NOLOGO"] = "1";
        psi.Environment["DOTNET_DbgEnableMiniDump"] = "0";
        psi.Environment["DOTNET_GCHeapHardLimit"] = "0x" + _limits.HeapLimitBytes.ToString("X", CultureInfo.InvariantCulture);

        using var process = new Process { StartInfo = psi };
        try
        {
            if (!process.Start())
                throw new CertificationHarnessException($"Execution harness: '{psi.FileName}' did not start the witness replay runner.");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new CertificationHarnessException(
                $"Execution harness: the dotnet host '{psi.FileName}' could not be started ({ex.Message}), so no candidate or "
                + "mutant code can be replayed. Fix: run the certifier through the dotnet muxer (Environment.ProcessPath), "
                + "or set DOTNET_HOST_PATH / DOTNET_ROOT to a .NET installation that carries the running runtime.", ex);
        }

        using var killOnCancel = cancellationToken.Register(() => TryKill(process));
        var stderrTail = new BoundedTail(2048);
        var stderrTask = DrainAsync(process.StandardError, stderrTail);
        var lineDeadline = _limits.PerCaseTimeout + _limits.ProgressGrace;
        var childClock = Stopwatch.StartNew();
        var sawReady = false;
        string? lastStarted = null;
        var killedForNoProgress = false;

        while (true)
        {
            var wait = budgetLeft - childClock.Elapsed;
            if (wait > lineDeadline)
                wait = lineDeadline;
            if (wait <= TimeSpan.Zero)
            {
                TryKill(process);
                await WaitForExitAsync(process, cancellationToken).ConfigureAwait(false);
                throw TotalBudgetExceeded(unexecuted);
            }

            var readTask = process.StandardOutput.ReadLineAsync(cancellationToken).AsTask();
            var winner = await Task.WhenAny(readTask, Task.Delay(wait, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (winner != readTask)
            {
                TryKill(process);
                Observe(readTask);
                if (budgetLeft - childClock.Elapsed <= TimeSpan.Zero)
                {
                    await WaitForExitAsync(process, cancellationToken).ConfigureAwait(false);
                    throw TotalBudgetExceeded(unexecuted);
                }

                killedForNoProgress = true;
                break;
            }

            var line = await readTask.ConfigureAwait(false);
            if (line is null)
                break; // EOF: the runner exited (or was killed by its own code).

            if (!line.StartsWith(nonce, StringComparison.Ordinal) || line.Length <= nonce.Length || line[nonce.Length] != ' ')
                continue; // Brick chatter that reached the raw stdout: not protocol.

            RunnerLine? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<RunnerLine>(line.AsSpan(nonce.Length + 1), JsonOptions);
            }
            catch (JsonException)
            {
                continue; // A line cut short by the process dying mid-write; the missing slot says the rest.
            }

            if (parsed is null)
                continue;

            switch (parsed.K)
            {
                case "ready":
                    sawReady = true;
                    break;
                case "unit":
                    lastStarted = parsed.U;
                    break;
                case "obs" when parsed.U is not null && observations.TryGetValue(parsed.U, out var slots):
                    slots[(parsed.C, parsed.R)] = new CandidateCaseObservation(
                        parsed.U, parsed.C, parsed.R, parsed.Threw, parsed.Err, parsed.Summary,
                        parsed.Threw ? null : DecodeAll(parsed.Out));
                    break;
            }
        }

        await WaitForExitAsync(process, cancellationToken).ConfigureAwait(false);
        try
        {
            await stderrTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // The tail we have is the tail we report.
        }

        return new ChildRun(process.ExitCode, sawReady, lastStarted, killedForNoProgress, stderrTail.ToSingleLine());
    }

    private static async Task WaitForExitAsync(Process process, CancellationToken cancellationToken)
    {
        // After stdout closes the runner exits on its own within moments; a runner that does not
        // (a brick hung in a ProcessExit handler) is killed. Either way this returns.
        using var grace = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        grace.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            await process.WaitForExitAsync(grace.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
        {
            // Already gone, or not ours to kill.
        }
    }

    private static void Observe(Task task) =>
        _ = task.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

    private static async Task DrainAsync(StreamReader reader, BoundedTail tail)
    {
        var buffer = new char[4096];
        try
        {
            int read;
            while ((read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                tail.Append(buffer.AsSpan(0, read));
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            // The process is gone; whatever was read is the tail.
        }
    }

    /// <summary>
    /// The runner this backend launches, materialised on first use into the backend's own scratch
    /// directory. Compiling it is the one Roslyn compile every certification pays that has nothing
    /// to do with the candidate — the same source against the same reference set — so the
    /// compiled image comes from <see cref="RunnerCompileCache"/> when this process has already
    /// compiled it for that exact set, and is compiled here otherwise. Internal so the cache's
    /// tests can drive the compile path without a job.
    /// </summary>
    internal async Task<string> EnsureRunnerAsync(CancellationToken cancellationToken)
    {
        if (_runnerPath is { } ready)
            return ready;

        await _runnerBuild.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_runnerPath is { } built)
                return built;

            var runnerDir = Path.Combine(_workDir, "runner");
            Directory.CreateDirectory(runnerDir);
            var dllPath = Path.Combine(runnerDir, WitnessReplayRunner.AssemblyName + ".dll");
            RunnerCompiledFresh = await Task.Run(
                () => RunnerCompileCache.Materialize(WitnessReplayRunner.Source, BuildRunnerReferences(_references), dllPath, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            await File.WriteAllTextAsync(
                Path.Combine(runnerDir, WitnessReplayRunner.AssemblyName + ".runtimeconfig.json"),
                RuntimeConfigJson(), cancellationToken).ConfigureAwait(false);
            _runnerPath = dllPath;
            return dllPath;
        }
        finally
        {
            _runnerBuild.Release();
        }
    }

    /// <summary>
    /// The reference set the runner compiles against: the brick's own set — the compiler's record
    /// of what the brick was built against, so the runner binds <c>Ashlar.Brick.Contracts</c>
    /// exactly as the brick does — plus the shared-framework assemblies the runner's own code needs.
    /// Order is deterministic (the brick's set in the order given, then
    /// <see cref="RunnerFrameworkAssemblies"/> in declared order), so the same inputs always hash
    /// to the same cache key.
    /// </summary>
    internal static List<MetadataReference> BuildRunnerReferences(IReadOnlyList<string> brickReferences)
    {
        var references = RoslynCodeAnalysisService.BuildReferenceSet(brickReferences);
        var present = new HashSet<string>(
            references.OfType<PortableExecutableReference>().Select(r => r.FilePath ?? string.Empty),
            StringComparer.OrdinalIgnoreCase);
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (runtimeDir is not null)
        {
            foreach (var name in RunnerFrameworkAssemblies)
            {
                var path = Path.Combine(runtimeDir, name + ".dll");
                if (File.Exists(path) && present.Add(path))
                    references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        return references;
    }

    private static void CompileRunner(
        string source, IReadOnlyList<MetadataReference> references, string dllPath, CancellationToken cancellationToken)
    {
        var tree = CSharpSyntaxTree.ParseText(
            source, new CSharpParseOptions(LanguageVersion.CSharp12), cancellationToken: cancellationToken);

        var compilation = CSharpCompilation.Create(
            WitnessReplayRunner.AssemblyName,
            [tree],
            references,
            new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Release,
                nullableContextOptions: NullableContextOptions.Disable));
        var emit = compilation.Emit(dllPath, cancellationToken: cancellationToken);
        if (emit.Success)
            return;

        var errors = emit.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Take(5)
            .Select(d => $"{d.Id}: {d.GetMessage(CultureInfo.InvariantCulture)}");
        throw new CertificationHarnessException(
            "Execution harness: the witness replay runner did not compile against the brick's reference set ("
            + string.Join("; ", errors) + "). The runner binds to Ashlar.Brick.Contracts exactly as the brick does, so "
            + "this means the request's CompilationReferences do not carry the brick contract assembly the runner needs. "
            + "Refusing rather than executing anything in the certifier's own process.");
    }

    /// <summary>
    /// The runner runs on the runtime that is running the certifier — the mutants were compiled
    /// against exactly its core assemblies — and that runtime is known to be installed because
    /// this process is on it.
    /// </summary>
    private static string RuntimeConfigJson()
    {
        var version = Environment.Version;
        return string.Create(CultureInfo.InvariantCulture,
            $$"""
            {
              "runtimeOptions": {
                "tfm": "net{{version.Major}}.0",
                "framework": { "name": "Microsoft.NETCore.App", "version": "{{version.Major}}.{{version.Minor}}.0" },
                "rollForward": "LatestPatch"
              }
            }
            """);
    }

    /// <summary>
    /// The dotnet muxer that should launch the runner: the one running this process when there is
    /// one, else the installation the running runtime lives in, else <c>dotnet</c> on PATH — the
    /// same assumption <see cref="EvaluatedBrickProject"/> makes for the brick build.
    /// </summary>
    internal static string LocateDotnetHost()
    {
        var muxer = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        static bool IsMuxer(string? path, string name) =>
            !string.IsNullOrEmpty(path)
            && string.Equals(Path.GetFileName(path), name, StringComparison.OrdinalIgnoreCase)
            && File.Exists(path);

        if (IsMuxer(Environment.ProcessPath, muxer))
            return Environment.ProcessPath!;

        var hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (IsMuxer(hostPath, muxer))
            return hostPath!;

        var root = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrEmpty(root) && File.Exists(Path.Combine(root, muxer)))
            return Path.Combine(root, muxer);

        // <root>/shared/Microsoft.NETCore.App/<version>/System.Private.CoreLib.dll → <root>/dotnet
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (runtimeDir is not null)
        {
            var derived = Path.GetFullPath(Path.Combine(runtimeDir, "..", "..", "..", muxer));
            if (File.Exists(derived))
                return derived;
        }

        return "dotnet";
    }

    private static async Task<(string AssemblyPath, string TypeName)> ResolveCandidateAsync(
        CertificationRequest request, string brickTypeName, string workDir, CancellationToken cancellationToken)
    {
        var brickType = request.Brick.GetType();
        var location = brickType.Assembly.Location;
        if (!string.IsNullOrEmpty(location) && File.Exists(location))
            return (location, brickType.FullName ?? brickTypeName);

        var candidateDir = Path.Combine(workDir, "candidate");
        var assemblyName = $"CandidateBrick_{Guid.NewGuid():N}";
        var outputPath = Path.Combine(candidateDir, assemblyName + ".dll");
        var compiler = new RoslynCodeAnalysisService(NullLogger<RoslynCodeAnalysisService>.Instance);
        var compile = await compiler.CompileAsync(
            CandidateSourceWrapper.Wrap(request.SourceCode), assemblyName, outputPath, request.CompilationReferences, cancellationToken)
            .ConfigureAwait(false);
        if (!compile.Success || string.IsNullOrWhiteSpace(compile.AssemblyPath) || !File.Exists(compile.AssemblyPath))
        {
            throw new CertificationHarnessException(
                $"Execution harness: the request's brick instance ({brickType.FullName}) was loaded from bytes, so there is no "
                + "assembly on disk to replay, and its SourceCode did not compile in the certifier's own compilation ("
                + string.Join("; ", compile.Errors.Take(5)) + "). The analyzer fence compiled that same text a moment ago, so "
                + "the reference set changed under the gate. Refusing rather than executing anything in-process.");
        }

        return (compile.AssemblyPath, brickTypeName);
    }

    /// <summary>
    /// Where the runner resolves assemblies from: the directories of every compile reference (the
    /// compiler's own record of what the brick was built against), the candidate's own directory,
    /// and the job directory holding the mutants. Reference-assembly folders (<c>ref/</c>) probe
    /// last — they carry no IL and the runner skips them, but a lib/ twin should win outright.
    /// </summary>
    private List<string> ProbeDirectories(IEnumerable<RunnerUnit> units)
    {
        var comparer = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var dirs = new List<string>();
        var seen = new HashSet<string>(comparer);
        foreach (var path in _references.Concat(units.Select(u => u.AssemblyPath)))
        {
            string? dir;
            try
            {
                dir = Path.GetDirectoryName(Path.GetFullPath(path));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(dir) && seen.Add(dir))
                dirs.Add(dir);
        }

        static bool IsReferenceFolder(string dir) =>
            dir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
               .Any(segment => string.Equals(segment, "ref", StringComparison.OrdinalIgnoreCase));

        return dirs.Where(d => !IsReferenceFolder(d)).Concat(dirs.Where(IsReferenceFolder)).ToList();
    }

    private static Dictionary<string, TypedValue> EncodeAll(IReadOnlyDictionary<string, object> values)
    {
        var encoded = new Dictionary<string, TypedValue>(StringComparer.Ordinal);
        foreach (var (key, value) in values)
            encoded[key] = Encode(value);
        return encoded;
    }

    /// <summary>Mirror of the runner's <c>Encode</c>: the CLR type rides along with the value.</summary>
    internal static TypedValue Encode(object? value) => value switch
    {
        null => new TypedValue("null", null),
        bool b => new TypedValue("bool", b ? "true" : "false"),
        int i => new TypedValue("i32", i.ToString(CultureInfo.InvariantCulture)),
        long l => new TypedValue("i64", l.ToString(CultureInfo.InvariantCulture)),
        short s => new TypedValue("i16", s.ToString(CultureInfo.InvariantCulture)),
        byte by => new TypedValue("u8", by.ToString(CultureInfo.InvariantCulture)),
        double d => new TypedValue("f64", d.ToString("R", CultureInfo.InvariantCulture)),
        float f => new TypedValue("f32", f.ToString("R", CultureInfo.InvariantCulture)),
        decimal m => new TypedValue("dec", m.ToString(CultureInfo.InvariantCulture)),
        string str => new TypedValue("str", str),
        JsonElement el => new TypedValue("json", el.GetRawText()),
        _ => EncodeOpaque(value),
    };

    private static TypedValue EncodeOpaque(object value)
    {
        try
        {
            return new TypedValue("json", JsonSerializer.Serialize(value, value.GetType()));
        }
        catch (Exception ex) when (ex is NotSupportedException or JsonException or InvalidOperationException)
        {
            return new TypedValue("str", Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
        }
    }

    private static Dictionary<string, object?>? DecodeAll(Dictionary<string, TypedValue>? values)
    {
        if (values is null)
            return null;

        var decoded = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in values)
            decoded[key] = Decode(value);
        return decoded;
    }

    /// <summary>Mirror of the runner's <c>Decode</c>: the exact CLR type the brick produced.</summary>
    internal static object? Decode(TypedValue value) => value.T switch
    {
        "null" => null,
        "bool" => value.V == "true",
        "i32" => int.Parse(value.V!, CultureInfo.InvariantCulture),
        "i64" => long.Parse(value.V!, CultureInfo.InvariantCulture),
        "i16" => short.Parse(value.V!, CultureInfo.InvariantCulture),
        "u8" => byte.Parse(value.V!, CultureInfo.InvariantCulture),
        "f64" => double.Parse(value.V!, CultureInfo.InvariantCulture),
        "f32" => float.Parse(value.V!, CultureInfo.InvariantCulture),
        "dec" => decimal.Parse(value.V!, CultureInfo.InvariantCulture),
        "str" => value.V,
        "json" => JsonDocument.Parse(value.V!).RootElement.Clone(),
        _ => throw new CertificationHarnessException(
            $"Execution harness: the witness replay runner sent a value tagged '{value.T}', which this certifier does not "
            + "understand. Runner and certifier are compiled from the same source, so the protocol drifted; refusing "
            + "rather than comparing a value it cannot decode."),
    };

    /// <summary>
    /// Process-scoped cache of compiled runners, keyed by CONTENT: the runner source, the runtime
    /// that will host it, and every reference in order — path and MVID. Two backends whose bricks
    /// were built against the same set share one compile; a set that differs by one path, one
    /// rebuilt assembly (same path, new MVID) or one character of runner source does not.
    /// </summary>
    /// <remarks>
    /// <para><b>Why a cache.</b> The runner is the one Roslyn compile in a certification that has
    /// nothing to do with the candidate: identical source against an identical reference set, one
    /// to two seconds of parse-bind-emit, once per certification — and a cert-gate run certifies
    /// hundreds of times. The key is the complete input of that compile and nothing else, so a hit
    /// is the image a fresh compile would have produced (up to Roslyn's per-emit MVID).</para>
    ///
    /// <para><b>Why process-scoped.</b> The directory carries the process id and start time and is
    /// removed on exit; nothing this process compiled is offered to another process, and nothing
    /// another process left behind is trusted here. A cross-process cache would also save the CLI's
    /// single compile per invocation, at the price of executing an image out of a temp directory
    /// this certifier did not write — a trade the gate does not make.</para>
    ///
    /// <para><b>Atomicity.</b> The image is copied into the cache under a staging name and moved
    /// onto its final name, so a reader never sees a half-written file; misses on the same key
    /// serialise on a per-key lock so a burst of identical backends compiles once and the rest
    /// copy.</para>
    /// </remarks>
    internal static class RunnerCompileCache
    {
        private static readonly Lazy<string> CacheDirectoryLazy = new(CreateCacheDirectory);
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> KeyLocks = new(StringComparer.Ordinal);

        /// <summary>
        /// MVIDs of shared-framework assemblies — immutable for the life of this process (the
        /// runtime has them mapped) — so the ~170 of them are read once, not once per certification.
        /// Anything outside the runtime directory is read every time: a brick's own references can
        /// be rebuilt between certifications, and a stale MVID would reuse across a different set.
        /// </summary>
        private static readonly ConcurrentDictionary<string, string> FrameworkMvids = new(StringComparer.OrdinalIgnoreCase);

        private static readonly string? RuntimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
        private static readonly ConcurrentDictionary<string, int> CompilesByKey = new(StringComparer.Ordinal);
        private static int _compileCount;

        /// <summary>Runner compiles this process has performed (misses). Hits do not count.</summary>
        internal static int CompileCount => Volatile.Read(ref _compileCount);

        /// <summary>
        /// Runner compiles this process has performed for one key. The per-key count is what a test
        /// can assert on: the process-wide total moves whenever another test collection compiles
        /// against another reference set, and the assembly runs collections in parallel.
        /// </summary>
        internal static int CompilesFor(string key) => CompilesByKey.TryGetValue(key, out var count) ? count : 0;

        /// <summary>Where this process keeps its compiled runners.</summary>
        internal static string CacheDirectory => CacheDirectoryLazy.Value;

        /// <summary>
        /// Puts a compiled runner for <paramref name="source"/> against <paramref name="references"/>
        /// at <paramref name="dllPath"/>. Returns <c>true</c> when it compiled (a miss) and
        /// <c>false</c> when it copied this process's earlier compile of the identical inputs (a hit).
        /// </summary>
        internal static bool Materialize(
            string source, IReadOnlyList<MetadataReference> references, string dllPath, CancellationToken cancellationToken)
        {
            var key = ComputeKey(source, references);
            var cached = Path.Combine(CacheDirectory, key + ".dll");
            var gate = KeyLocks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
            gate.Wait(cancellationToken);
            try
            {
                if (File.Exists(cached))
                {
                    try
                    {
                        File.Copy(cached, dllPath, overwrite: true);
                        return false;
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        // The image went away between the check and the copy — a temp sweeper, a
                        // full disk. A miss is always safe; a failed hit must never be a failed
                        // certification.
                    }
                }

                CompileRunner(source, references, dllPath, cancellationToken);
                Interlocked.Increment(ref _compileCount);
                CompilesByKey.AddOrUpdate(key, 1, static (_, count) => count + 1);

                var staging = cached + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.Copy(dllPath, staging);
                File.Move(staging, cached, overwrite: true);
                return true;
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>
        /// SHA-256 over the runner source, the hosting runtime, and every reference in the order the
        /// compiler sees it — full path and MVID, so a rebuilt assembly at the same path is another
        /// key. A reference that is not a file on disk cannot be keyed by content and never hits.
        /// </summary>
        internal static string ComputeKey(string source, IReadOnlyList<MetadataReference> references)
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            Append(hash, "source", source);
            Append(hash, "runtime", RuntimeInformation.FrameworkDescription);
            Append(hash, "runtime-version", Environment.Version.ToString());
            Append(hash, "rid", RuntimeInformation.RuntimeIdentifier);
            Append(hash, "references", references.Count.ToString(CultureInfo.InvariantCulture));
            foreach (var reference in references)
            {
                var path = (reference as PortableExecutableReference)?.FilePath;
                if (string.IsNullOrEmpty(path))
                {
                    Append(hash, "reference-unkeyable", reference.Display + "\n" + Guid.NewGuid().ToString("N"));
                    continue;
                }

                var fullPath = Path.GetFullPath(path);
                Append(hash, "reference", fullPath + "\n" + MvidOf(fullPath));
            }

            return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        }

        private static void Append(IncrementalHash hash, string label, string value)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(label));
            hash.AppendData([0]);
            hash.AppendData(Encoding.UTF8.GetBytes(value));
            hash.AppendData([0]);
        }

        /// <summary>
        /// The module version id the compiler stamped into the assembly at <paramref name="path"/>
        /// — a fresh GUID per emit, so it identifies the build, not the file name. A file without
        /// metadata or that cannot be read is keyed by its length and write time instead.
        /// </summary>
        internal static string MvidOf(string path)
        {
            var isFramework = RuntimeDirectory is not null
                && string.Equals(Path.GetDirectoryName(path), RuntimeDirectory, StringComparison.OrdinalIgnoreCase);
            if (isFramework && FrameworkMvids.TryGetValue(path, out var known))
                return known;

            string mvid;
            try
            {
                using var stream = File.OpenRead(path);
                using var pe = new PEReader(stream);
                if (pe.HasMetadata)
                {
                    var metadata = pe.GetMetadataReader();
                    mvid = metadata.GetGuid(metadata.GetModuleDefinition().Mvid).ToString("N");
                }
                else
                {
                    mvid = Stamp("no-metadata", path);
                }
            }
            catch (Exception ex) when (ex is IOException or BadImageFormatException or UnauthorizedAccessException or InvalidOperationException)
            {
                mvid = Stamp("unreadable", path);
            }

            if (isFramework)
                FrameworkMvids[path] = mvid;
            return mvid;

            static string Stamp(string kind, string path)
            {
                var info = new FileInfo(path);
                return string.Create(CultureInfo.InvariantCulture, $"{kind}:{info.Length}:{info.LastWriteTimeUtc.Ticks}");
            }
        }

        private static string CreateCacheDirectory()
        {
            long started;
            try
            {
                started = Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks;
            }
            catch (Exception ex) when (ex is InvalidOperationException or PlatformNotSupportedException or System.ComponentModel.Win32Exception)
            {
                started = Environment.TickCount64;
            }

            var directory = Path.Combine(
                Path.GetTempPath(), "ashlar-cert-exec", "runner-cache",
                string.Create(CultureInfo.InvariantCulture, $"{Environment.ProcessId}-{started}"));
            Directory.CreateDirectory(directory);
            // Best effort, like the backends' own scratch directories: nothing in it is meant to
            // outlive this process, and a stale directory can never be read by another (the id and
            // start time are in the path).
            AppDomain.CurrentDomain.ProcessExit += (_, _) => TryDeleteDirectory(directory);
            return directory;
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort: a file still mapped by a dying child is reclaimed by the OS temp policy.
        }
    }

    /// <summary>Keeps the last <c>capacity</c> characters written to it.</summary>
    private sealed class BoundedTail(int capacity)
    {
        private readonly StringBuilder _buffer = new();

        public void Append(ReadOnlySpan<char> text)
        {
            _buffer.Append(text);
            if (_buffer.Length > capacity)
                _buffer.Remove(0, _buffer.Length - capacity);
        }

        /// <summary>The tail flattened to one line, so it can ride inside an observation's error.</summary>
        public string ToSingleLine()
        {
            var text = _buffer.ToString().Trim();
            if (text.Length == 0)
                return string.Empty;
            var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            // The first lines say what happened ("Stack overflow.", "Unhandled exception. ..."); the
            // rest is a stack trace that can run to thousands of frames.
            var head = string.Join(" | ", lines.Take(3));
            return head.Length <= 400 ? head : head[..400] + "…";
        }
    }

    private sealed record ChildRun(int ExitCode, bool SawReady, string? LastStartedUnitId, bool KilledForNoProgress, string StderrTail);

    private sealed record RunnerJob(
        string Nonce,
        IReadOnlyList<string> ProbeDirs,
        IReadOnlyList<RunnerUnit> Units,
        IReadOnlyList<RunnerCase> Cases,
        int Repeats,
        int PerCaseTimeoutMs,
        int PerUnitTimeoutMs);

    private sealed record RunnerUnit(string UnitId, string AssemblyPath, string TypeName, string BrickId);

    private sealed record RunnerCase(Dictionary<string, TypedValue> Inputs);

    /// <summary>A value with its CLR type, as it crosses the process boundary.</summary>
    internal sealed record TypedValue(string T, string? V);

    private sealed record RunnerLine(
        string K, string? U, int C, int R, bool Threw, string? Err, string? Summary, Dictionary<string, TypedValue>? Out);
}
