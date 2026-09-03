namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// The child program that replays witness cases against the candidate and its mutants for
/// <see cref="LocalProcessExecutionBackend"/>. Held as source, compiled once per certification
/// with the same Roslyn machinery and the same reference set as the mutants themselves, so it
/// needs no project, no package asset and no restore.
/// </summary>
/// <remarks>
/// <para>The runner is deliberately DUMB: it executes and reports. It holds no expected outputs,
/// no comparers and no thresholds — the gate judges. What it does own is the one thing the
/// certifying process must never do itself: RUN author code. A mutant that loops forever times
/// out here and the process exits to shed the spinning thread; a mutant that overflows the
/// stack, calls <c>Environment.Exit</c> or <c>FailFast</c>, throws on a background thread or
/// allocates without bound kills this process, and the certifier reads the exit code and
/// stderr, records the crash against the unit that was executing, and restarts the runner for
/// the units that remain.</para>
///
/// <para>Protocol: one line per event on stdout, each prefixed by the job's nonce and a space,
/// then a JSON object: <c>{"k":"ready"}</c> once the job is loaded, <c>{"k":"unit","u":id}</c>
/// before a unit is loaded, and <c>{"k":"obs",…}</c> per (unit, case, repeat). Lines without the
/// prefix are brick chatter and are ignored — the runner redirects <c>Console.Out</c> to stderr
/// so a brick's <c>Console.WriteLine</c> cannot corrupt the stream by accident. Values cross the
/// boundary TYPED (<c>{"t":"i32","v":"5"}</c>) in both directions, because
/// <c>BrickInput.Get&lt;T&gt;</c> is an exact type check and the witness comparers distinguish
/// integral from floating values: a plain JSON number would turn every <c>int</c> into a
/// <c>long</c> and every <c>3.0</c> into a <c>3</c>, and honest bricks would fail.</para>
/// </remarks>
internal static class WitnessReplayRunner
{
    /// <summary>The runner's assembly name; also the file stem of its runtimeconfig.</summary>
    public const string AssemblyName = "AshlarWitnessReplayRunner";

    /// <summary>
    /// The exit code the runner uses when it ends itself after a unit timed out. Distinct from a
    /// crash: the unit's slots are already reported, and the process ends only to be rid of the
    /// still-running execution. The certifier resumes with the next unit.
    /// </summary>
    public const int ExitTimedOut = 3;

    /// <summary>The runner program. Internal so tests can pin the markers the gate parses out of it.</summary>
    internal const string Source = """
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// Ashlar witness replay runner. argv[0] = job.json. It executes and reports; it holds no expected
// outputs and judges nothing. See WitnessReplayRunner in Ashlar.Infrastructure for the protocol.
internal static class Program
{
    private const int ExitOk = 0;
    private const int ExitUsage = 2;
    private const int ExitTimedOut = 3;

    // Must match ExecutionRunnerMarkers in Ashlar.Infrastructure; SessionExecutionBackendTests-style pins hold them together.
    private const string TimeoutPrefix = "execution timed out";
    private const string LoadFailurePrefix = "unit load failed: ";

    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static TextWriter _protocol;
    private static string _nonce;

    private static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: AshlarWitnessReplayRunner <job.json>");
            return ExitUsage;
        }

        // Protocol lines go to the raw stdout the certifier reads. Anything a brick writes through
        // Console.Out goes to stderr instead, so brick chatter cannot corrupt the protocol stream.
        _protocol = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
        Console.SetOut(Console.Error);

        Job job;
        try
        {
            job = JsonSerializer.Deserialize<Job>(File.ReadAllText(args[0]), Json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("job file unreadable: " + ex.Message);
            return ExitUsage;
        }

        if (job is null || job.Units is null || job.Cases is null || string.IsNullOrEmpty(job.Nonce))
        {
            Console.Error.WriteLine("job file incomplete");
            return ExitUsage;
        }

        _nonce = job.Nonce;
        var probeDirs = job.ProbeDirs ?? new List<string>();
        // Registered BEFORE any method that touches the brick contract types is compiled: Main
        // itself names none of them, so the runner's own dependency on Ashlar.Brick.Contracts
        // resolves through this handler like every brick's does.
        AssemblyLoadContext.Default.Resolving += (context, name) => Resolve(context, name, probeDirs);

        Emit(new Line("ready", null, 0, 0, false, null, null, null));
        var code = Run(job);
        _protocol.Flush();
        // Explicit: a brick that left a foreground thread running must not keep this process alive.
        Environment.Exit(code);
        return code;
    }

    private static int Run(Job job)
    {
        var context = new ReplayExecutionContext();
        foreach (var unit in job.Units)
        {
            Emit(new Line("unit", unit.UnitId, 0, 0, false, null, null, null));

            Ashlar.Core.Domain.Bricks.Brick brick = null;
            string unitFault = null;
            try
            {
                brick = LoadBrick(unit);
            }
            catch (TargetInvocationException ex)
            {
                // The brick's own constructor threw: a fact about the brick, reported on every case.
                unitFault = "constructor threw: " + (ex.InnerException ?? ex).Message;
            }
            catch (Exception ex)
            {
                // The runner could not load or construct the type at all: a harness fact, marked so
                // the gate refuses it instead of scoring it as a kill.
                unitFault = LoadFailurePrefix + ex.GetBaseException().Message;
            }

            var clock = Stopwatch.StartNew();
            string timedOut = null;
            for (var caseIndex = 0; caseIndex < job.Cases.Count; caseIndex++)
            {
                for (var repeat = 0; repeat < job.Repeats; repeat++)
                {
                    if (unitFault is not null)
                    {
                        Emit(Fault(unit, caseIndex, repeat, unitFault));
                        continue;
                    }

                    if (timedOut is not null)
                    {
                        Emit(Fault(unit, caseIndex, repeat, timedOut));
                        continue;
                    }

                    if (clock.ElapsedMilliseconds > job.PerUnitTimeoutMs)
                    {
                        timedOut = TimeoutPrefix + ": the unit used " + clock.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture)
                            + " ms of its " + job.PerUnitTimeoutMs.ToString(CultureInfo.InvariantCulture)
                            + " ms budget (perUnitTimeoutMs) before case " + caseIndex.ToString(CultureInfo.InvariantCulture)
                            + " repeat " + repeat.ToString(CultureInfo.InvariantCulture);
                        Emit(Fault(unit, caseIndex, repeat, timedOut));
                        continue;
                    }

                    Dictionary<string, object> inputs;
                    try
                    {
                        inputs = Decode(job.Cases[caseIndex].Inputs);
                    }
                    catch (Exception ex)
                    {
                        Emit(Fault(unit, caseIndex, repeat, LoadFailurePrefix + "witness inputs undecodable: " + ex.Message));
                        continue;
                    }

                    // The ENTIRE invocation runs on a pool thread: a synchronous brick (Task.FromResult
                    // shape) executes its body inside the call itself, so a nonterminating body would
                    // otherwise spin on this thread and the timeout race could never start.
                    var work = Task.Run(() => Execute(brick, inputs, context));
                    var finished = Task.WhenAny(work, Task.Delay(job.PerCaseTimeoutMs)).GetAwaiter().GetResult() == work;
                    if (!finished)
                    {
                        timedOut = TimeoutPrefix + " after " + job.PerCaseTimeoutMs.ToString(CultureInfo.InvariantCulture)
                            + " ms (perCaseTimeoutMs) on case " + caseIndex.ToString(CultureInfo.InvariantCulture)
                            + " repeat " + repeat.ToString(CultureInfo.InvariantCulture);
                        Emit(Fault(unit, caseIndex, repeat, timedOut));
                        continue;
                    }

                    try
                    {
                        var result = work.GetAwaiter().GetResult();
                        Emit(new Line("obs", unit.UnitId, caseIndex, repeat, false, null, result.Summary, result.Outputs));
                    }
                    catch (Exception ex)
                    {
                        // The brick's exception, unwrapped: ExecuteAsync is called directly, not reflectively.
                        Emit(Fault(unit, caseIndex, repeat, ex.Message));
                    }
                }
            }

            if (timedOut is not null)
            {
                // The timed-out execution is still running on a pool thread and nothing can stop it.
                // Ending the process is the only way to be rid of it; the certifier restarts the
                // runner for the units that remain.
                _protocol.Flush();
                return ExitTimedOut;
            }
        }

        return ExitOk;
    }

    private static ExecutionResult Execute(
        Ashlar.Core.Domain.Bricks.Brick brick,
        Dictionary<string, object> inputs,
        Ashlar.Core.Domain.Execution.IExecutionContext context)
    {
        var input = new Ashlar.Core.Domain.Execution.BrickInput(inputs);
        var task = brick.ExecuteAsync(input, Ashlar.Core.Domain.Bricks.ImplementationType.Deterministic, context, CancellationToken.None);
        var output = task.GetAwaiter().GetResult();
        if (output is null)
            throw new InvalidOperationException("execution returned null output");

        var encoded = new Dictionary<string, Typed>(StringComparer.Ordinal);
        foreach (var entry in output.ToDictionary())
            encoded[entry.Key] = Encode(entry.Value);
        return new ExecutionResult(output.Summary, encoded);
    }

    private static Ashlar.Core.Domain.Bricks.Brick LoadBrick(Unit unit)
    {
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(unit.AssemblyPath));
        var type = assembly.GetType(unit.TypeName, throwOnError: false)
            ?? assembly.GetTypes().FirstOrDefault(t =>
                t.IsClass && !t.IsAbstract && typeof(Ashlar.Core.Domain.Bricks.Brick).IsAssignableFrom(t))
            ?? throw new InvalidOperationException(
                "no brick type '" + unit.TypeName + "' in " + Path.GetFileName(unit.AssemblyPath));

        // Bricks construct with no arguments. The one tolerated exception takes the brick id — the
        // autonomy loop's identity-only handle — so a request carrying such a stand-in still gets the
        // stand-in's own refusal on every case rather than a harness fault.
        object instance;
        var parameterless = type.GetConstructor(Type.EmptyTypes);
        if (parameterless is not null)
        {
            instance = parameterless.Invoke(null);
        }
        else
        {
            var byId = type.GetConstructor(new[] { typeof(string) })
                ?? throw new MissingMethodException(
                    "'" + type.FullName + "' has neither a parameterless constructor nor one taking the brick id");
            instance = byId.Invoke(new object[] { unit.BrickId });
        }

        return instance as Ashlar.Core.Domain.Bricks.Brick
            ?? throw new InvalidOperationException("'" + type.FullName + "' is not an Ashlar brick");
    }

    private static Assembly Resolve(AssemblyLoadContext context, AssemblyName name, IReadOnlyList<string> probeDirs)
    {
        foreach (var dir in probeDirs)
        {
            var candidate = Path.Combine(dir, name.Name + ".dll");
            if (!File.Exists(candidate))
                continue;

            try
            {
                return context.LoadFromAssemblyPath(candidate);
            }
            catch (BadImageFormatException)
            {
                // A reference assembly, or a native file under a managed name: keep probing.
            }
            catch (FileLoadException)
            {
            }
        }

        return null;
    }

    private static Typed Encode(object value)
    {
        switch (value)
        {
            case null: return new Typed("null", null);
            case bool b: return new Typed("bool", b ? "true" : "false");
            case int i: return new Typed("i32", i.ToString(CultureInfo.InvariantCulture));
            case long l: return new Typed("i64", l.ToString(CultureInfo.InvariantCulture));
            case short s: return new Typed("i16", s.ToString(CultureInfo.InvariantCulture));
            case byte by: return new Typed("u8", by.ToString(CultureInfo.InvariantCulture));
            case double d: return new Typed("f64", d.ToString("R", CultureInfo.InvariantCulture));
            case float f: return new Typed("f32", f.ToString("R", CultureInfo.InvariantCulture));
            case decimal m: return new Typed("dec", m.ToString(CultureInfo.InvariantCulture));
            case string str: return new Typed("str", str);
            default:
                try
                {
                    return new Typed("json", JsonSerializer.Serialize(value, value.GetType(), Json));
                }
                catch (Exception)
                {
                    return new Typed("str", Convert.ToString(value, CultureInfo.InvariantCulture));
                }
        }
    }

    private static Dictionary<string, object> Decode(Dictionary<string, Typed> inputs)
    {
        var decoded = new Dictionary<string, object>(StringComparer.Ordinal);
        if (inputs is null)
            return decoded;
        foreach (var entry in inputs)
            decoded[entry.Key] = Decode(entry.Value);
        return decoded;
    }

    private static object Decode(Typed value)
    {
        switch (value.T)
        {
            case "null": return null;
            case "bool": return value.V == "true";
            case "i32": return int.Parse(value.V, CultureInfo.InvariantCulture);
            case "i64": return long.Parse(value.V, CultureInfo.InvariantCulture);
            case "i16": return short.Parse(value.V, CultureInfo.InvariantCulture);
            case "u8": return byte.Parse(value.V, CultureInfo.InvariantCulture);
            case "f64": return double.Parse(value.V, CultureInfo.InvariantCulture);
            case "f32": return float.Parse(value.V, CultureInfo.InvariantCulture);
            case "dec": return decimal.Parse(value.V, CultureInfo.InvariantCulture);
            case "str": return value.V;
            case "json": return JsonDocument.Parse(value.V).RootElement.Clone();
            default: throw new InvalidOperationException("unknown value tag '" + value.T + "'");
        }
    }

    private static Line Fault(Unit unit, int caseIndex, int repeat, string error) =>
        new Line("obs", unit.UnitId, caseIndex, repeat, true, error, null, null);

    private static void Emit(Line line)
    {
        _protocol.Write(_nonce);
        _protocol.Write(' ');
        _protocol.WriteLine(JsonSerializer.Serialize(line, Json));
    }

    // Same values as the certifier's own AuditExecutionContext.
    private sealed class ReplayExecutionContext : Ashlar.Core.Domain.Execution.IExecutionContext
    {
        public string AgentId => "cert-gate";
        public string BehaviorId => "cert-gate";
        public bool IsAirGapped => true;
        public bool AuditMode => true;
        public string Provider => "deterministic";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }

    internal sealed record ExecutionResult(string Summary, Dictionary<string, Typed> Outputs);
    internal sealed record Job(
        string Nonce, List<string> ProbeDirs, List<Unit> Units, List<Case> Cases, int Repeats, int PerCaseTimeoutMs, int PerUnitTimeoutMs);
    internal sealed record Unit(string UnitId, string AssemblyPath, string TypeName, string BrickId);
    internal sealed record Case(Dictionary<string, Typed> Inputs);
    internal sealed record Typed(string T, string V);
    internal sealed record Line(
        string K, string U, int C, int R, bool Threw, string Err, string Summary, Dictionary<string, Typed> Out);
}
""";
}
