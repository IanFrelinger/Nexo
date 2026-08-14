using System.Text;
using System.Text.Json;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Core.Application.Execution.Ports;

namespace Nexo.Infrastructure.Certification.HotSwap;

/// <summary>
/// Executes candidate and mutant code INSIDE the iteration's attested sandbox session —
/// the containment half the in-session build (P3) did not cover. A dumb runner is built
/// once per session from embedded source (zero package references, offline restore, same
/// discipline as the candidate build) and then fed batched jobs: assemblies, witness case
/// INPUTS, and a repeat count. It loads each unit in its own load context (fresh statics,
/// mirroring the in-process mutant isolation), executes reflectively using the
/// wrapper-injected <c>CertAuditContext</c> as the execution context, and prints raw
/// observations as JSON. It never sees expected outputs and never judges — the gate does.
///
/// <para>Fail-closed: any infrastructure failure (runner build, upload, nonzero exit,
/// unparseable output) throws. Fabricating observations — in either direction — would let
/// an infrastructure fault mint a verdict.</para>
/// </summary>
public sealed class SessionExecutionBackend : ICandidateExecutionBackend
{
    private const string ExecDir = "/nexo-exec";
    private const string RunnerDir = ExecDir + "/runner";
    private const string UnitsDir = ExecDir + "/units";
    private const string RunnerDll = RunnerDir + "/bin/Release/net9.0/SessionExecutionRunner.dll";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ISandboxedSession _session;
    private readonly string _candidateAssemblyPath;
    private bool _runnerReady;

    /// <summary>Creates the backend over a live session whose candidate build already ran.</summary>
    public SessionExecutionBackend(
        ISandboxedSession session,
        string candidateAssemblyPath = SessionCandidateBuild.BuiltAssemblyPath)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _candidateAssemblyPath = candidateAssemblyPath;
    }

    /// <inheritdoc />
    public string Describe() => $"in-session:{_session.SessionId}";

    /// <inheritdoc />
    public async Task<CandidateExecutionReport> ExecuteAsync(
        CandidateExecutionJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        await EnsureRunnerAsync(cancellationToken).ConfigureAwait(false);

        // Fresh unit scratch per job so runs cannot observe each other.
        await ExecOrThrowAsync(
            new[] { "sh", "-c", $"rm -rf {UnitsDir} && mkdir -p {UnitsDir}" },
            "resetting the execution unit directory", cancellationToken).ConfigureAwait(false);

        var unitPaths = new Dictionary<string, string?>();
        var index = 0;
        foreach (var unit in job.Units)
        {
            if (unit.Assembly is null)
            {
                unitPaths[unit.UnitId] = null; // The session-built candidate.
                continue;
            }

            var path = $"{UnitsDir}/unit-{index++}.dll";
            await UploadOrThrowAsync(path, unit.Assembly, $"uploading unit '{unit.UnitId}'", cancellationToken)
                .ConfigureAwait(false);
            unitPaths[unit.UnitId] = path;
        }

        var jobPayload = new RunnerJob(
            _candidateAssemblyPath,
            new[] { SessionCandidateBuild.BuiltAssemblyDirectory },
            job.Units.Select(u => new RunnerUnit(u.UnitId, unitPaths[u.UnitId], u.TypeName)).ToArray(),
            job.Witness.Cases.Select(c => new RunnerCase(c.Input)).ToArray(),
            job.Repeats);
        await UploadOrThrowAsync(
            $"{ExecDir}/job.json",
            JsonSerializer.SerializeToUtf8Bytes(jobPayload, JsonOptions),
            "uploading the execution job", cancellationToken).ConfigureAwait(false);

        var run = await _session.ExecAsync(
            new[] { "dotnet", RunnerDll, $"{ExecDir}/job.json" }, cancellationToken).ConfigureAwait(false);
        if (!run.Succeeded)
        {
            throw new InvalidOperationException(
                $"Session execution runner failed (exit {run.ExitCode}) in session '{_session.SessionId}': "
                + Tail(run.StdErr + run.StdOut));
        }

        RunnerObservation[]? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<RunnerObservation[]>(run.StdOut, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Session execution runner returned unparseable output in session '{_session.SessionId}': "
                + $"{ex.Message}; output tail: {Tail(run.StdOut)}", ex);
        }

        if (parsed is null)
        {
            throw new InvalidOperationException(
                $"Session execution runner returned no observations in session '{_session.SessionId}'.");
        }

        return new CandidateExecutionReport(parsed
            .Select(o => new CandidateCaseObservation(
                o.UnitId, o.CaseIndex, o.Repeat, o.Threw, o.Error, o.Summary,
                o.Outputs?.ToDictionary(kv => kv.Key, kv => (object?)kv.Value)))
            .ToArray());
    }

    private async Task EnsureRunnerAsync(CancellationToken cancellationToken)
    {
        if (_runnerReady)
            return;

        await ExecOrThrowAsync(
            new[] { "sh", "-c", $"rm -rf {RunnerDir} && mkdir -p {RunnerDir}" },
            "resetting the runner directory", cancellationToken).ConfigureAwait(false);
        await UploadOrThrowAsync(
            $"{RunnerDir}/Program.cs", Encoding.UTF8.GetBytes(RunnerSource),
            "uploading the runner source", cancellationToken).ConfigureAwait(false);
        await UploadOrThrowAsync(
            $"{RunnerDir}/SessionExecutionRunner.csproj", Encoding.UTF8.GetBytes(RunnerProject),
            "uploading the runner project", cancellationToken).ConfigureAwait(false);
        await UploadOrThrowAsync(
            $"{RunnerDir}/NuGet.config", Encoding.UTF8.GetBytes(SessionCandidateBuild.NuGetConfig),
            "uploading the runner NuGet.config", cancellationToken).ConfigureAwait(false);

        var build = await _session.ExecAsync(
            new[] { "dotnet", "build", $"{RunnerDir}/SessionExecutionRunner.csproj", "-c", "Release", "--nologo", "-v", "quiet" },
            cancellationToken).ConfigureAwait(false);
        if (!build.Succeeded)
        {
            throw new InvalidOperationException(
                $"Session execution runner failed to build in session '{_session.SessionId}' "
                + $"(exit {build.ExitCode}): {Tail(build.StdErr + build.StdOut)}");
        }

        _runnerReady = true;
    }

    private async Task ExecOrThrowAsync(
        IReadOnlyList<string> command, string what, CancellationToken cancellationToken)
    {
        var result = await _session.ExecAsync(command, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Session execution backend: {what} failed (exit {result.ExitCode}) in session "
                + $"'{_session.SessionId}': {Tail(result.StdErr + result.StdOut)}");
        }
    }

    private async Task UploadOrThrowAsync(
        string path, byte[] bytes, string what, CancellationToken cancellationToken)
    {
        var failure = await SessionFileTransfer.UploadAsync(_session, path, bytes, cancellationToken)
            .ConfigureAwait(false);
        if (failure is not null)
        {
            throw new InvalidOperationException(
                $"Session execution backend: {what} failed (exit {failure.ExitCode}) in session "
                + $"'{_session.SessionId}': {Tail(failure.StdErr + failure.StdOut)}");
        }
    }

    private static string Tail(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length <= 2000 ? trimmed : "…" + trimmed[^2000..];
    }

    private sealed record RunnerJob(
        string CandidatePath,
        IReadOnlyList<string> ProbeDirs,
        IReadOnlyList<RunnerUnit> Units,
        IReadOnlyList<RunnerCase> Cases,
        int Repeats);

    private sealed record RunnerUnit(string UnitId, string? AssemblyPath, string TypeName);

    private sealed record RunnerCase(IReadOnlyDictionary<string, object> Inputs);

    private sealed record RunnerObservation(
        string UnitId,
        int CaseIndex,
        int Repeat,
        bool Threw,
        string? Error,
        string? Summary,
        Dictionary<string, JsonElement>? Outputs);

    // The runner: deliberately DUMB. It executes and reports; it holds no expected
    // outputs, no thresholds, no comparers. Compiled in-session with zero package
    // references (System.Text.Json is inbox on net9.0), so restore is offline-safe.
    private const string RunnerProject = """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>disable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
""";

    private const string RunnerSource = """
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

// Session execution runner: argv[0] = job.json path. Prints a JSON observation array to
// stdout; diagnostics go to stderr. Exit 0 = the batch ran (individual case throws are
// observations, not failures); nonzero = runner infrastructure failure.
var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var job = JsonSerializer.Deserialize<Job>(File.ReadAllText(args[0]), jsonOptions);
if (job is null)
{
    Console.Error.WriteLine("job.json deserialized to null");
    return 2;
}

var observations = new List<Observation>();
foreach (var unit in job.Units)
{
    var assemblyPath = unit.AssemblyPath ?? job.CandidatePath;
    var probeDirs = job.ProbeDirs.Append(Path.GetDirectoryName(assemblyPath)).ToArray();

    Assembly assembly = null;
    object instance = null;
    MethodInfo execute = null;
    object auditContext = null;
    Type inputType = null, implType = null;
    string loadError = null;
    try
    {
        // One load context per unit: fresh statics, mirroring in-process mutant isolation.
        var context = new AssemblyLoadContext(unit.UnitId, isCollectible: false);
        context.Resolving += (ctx, name) =>
        {
            foreach (var dir in probeDirs)
            {
                var candidate = Path.Combine(dir ?? ".", name.Name + ".dll");
                if (File.Exists(candidate))
                    return ctx.LoadFromAssemblyPath(candidate);
            }
            return null;
        };
        assembly = context.LoadFromAssemblyPath(assemblyPath);
        var type = assembly.GetType(unit.TypeName)
            ?? assembly.GetTypes().FirstOrDefault(t => t.IsClass && !t.IsAbstract && t.Name != "CertAuditContext");
        if (type is null)
            throw new InvalidOperationException($"no brick type found for '{unit.TypeName}'");

        instance = Activator.CreateInstance(type);
        execute = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == "ExecuteAsync" && m.GetParameters().Length == 4);
        var parameters = execute.GetParameters();
        inputType = parameters[0].ParameterType;
        implType = parameters[1].ParameterType;
        // The certification wrapper injects CertAuditContext into every candidate/mutant
        // compile — the runner's execution context comes from the unit itself.
        auditContext = Activator.CreateInstance(
            assembly.GetTypes().First(t => t.Name == "CertAuditContext"));
    }
    catch (Exception ex)
    {
        loadError = $"unit load failed: {ex.GetBaseException().Message}";
    }

    for (var caseIndex = 0; caseIndex < job.Cases.Count; caseIndex++)
    {
        for (var repeat = 0; repeat < job.Repeats; repeat++)
        {
            if (loadError is not null)
            {
                observations.Add(new Observation(unit.UnitId, caseIndex, repeat, true, loadError, null, null));
                continue;
            }

            try
            {
                var inputs = new Dictionary<string, object>();
                foreach (var (key, value) in job.Cases[caseIndex].Inputs)
                    inputs[key] = Coerce(value);

                var brickInput = Activator.CreateInstance(inputType, inputs);
                var implementation = Enum.Parse(implType, "Deterministic");
                var task = (Task)execute.Invoke(instance, new[] { brickInput, implementation, auditContext, (object)CancellationToken.None });
                await task.ConfigureAwait(false);
                var output = task.GetType().GetProperty("Result").GetValue(task);
                if (output is null)
                {
                    observations.Add(new Observation(unit.UnitId, caseIndex, repeat, true, "execution returned null output", null, null));
                    continue;
                }

                var summary = output.GetType().GetProperty("Summary")?.GetValue(output) as string;
                var raw = output.GetType().GetMethod("ToDictionary").Invoke(output, null);
                var outputs = new Dictionary<string, object>();
                foreach (var entry in (IReadOnlyDictionary<string, object>)raw)
                    outputs[entry.Key] = entry.Value;

                observations.Add(new Observation(unit.UnitId, caseIndex, repeat, false, null, summary, outputs));
            }
            catch (Exception ex)
            {
                observations.Add(new Observation(
                    unit.UnitId, caseIndex, repeat, true, ex.GetBaseException().Message, null, null));
            }
        }
    }
}

Console.WriteLine(JsonSerializer.Serialize(observations, jsonOptions));
return 0;

static object Coerce(JsonElement el) => el.ValueKind switch
{
    JsonValueKind.String => el.GetString(),
    JsonValueKind.Number when el.TryGetInt64(out var l) => l,
    JsonValueKind.Number => el.GetDouble(),
    JsonValueKind.True => true,
    JsonValueKind.False => false,
    JsonValueKind.Null => null,
    _ => el.GetRawText()
};

internal sealed record Job(
    string CandidatePath,
    IReadOnlyList<string> ProbeDirs,
    IReadOnlyList<Unit> Units,
    IReadOnlyList<Case> Cases,
    int Repeats);
internal sealed record Unit(string UnitId, string AssemblyPath, string TypeName);
internal sealed record Case(Dictionary<string, JsonElement> Inputs);
internal sealed record Observation(
    string UnitId, int CaseIndex, int Repeat, bool Threw, string Error, string Summary, Dictionary<string, object> Outputs);
""";
}
