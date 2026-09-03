using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Testing.CodeAnalysis;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Neither child process the gate spawns — the author's build, the witness replay runner — sees
/// the certifier's environment. Each gets an explicit allowlist, and the signing keys are never
/// on it.
///
/// <para><b>Why this exists.</b> Confirmed by reading at 73a704a8: <c>EvaluatedBrickProject.Invoke</c>
/// removed eleven MSBuild property names from the inherited environment and passed the rest;
/// <c>LocalProcessExecutionBackend.RunChildAsync</c> removed <c>DOTNET_STARTUP_HOOKS</c> and passed
/// the rest. The record signing keys live in environment variables
/// (<see cref="CertificationRecordSigning.HmacKeyEnvVar"/>,
/// <see cref="CertificationRecordEd25519.PrivateKeyEnvVar"/>), so an <c>&lt;Exec&gt;</c> target in
/// the brick's own <c>.csproj</c>, or the candidate's own <c>ExecuteAsync</c>, could read the key
/// that was about to sign its certificate and mint a record — over any source text — that
/// verified under the operator's key. A record-forgery primitive, handed to the author by the
/// certifier.</para>
///
/// <para><b>What is pinned.</b> Three views of the same fact, each red at 73a704a8: the
/// environment a build target actually sees (dumped to a file by the fixture project), the
/// environment the runner's candidate actually sees (returned as brick outputs), and the end
/// state — a candidate that forges a record from inside the runner gets one the verifier
/// rejects. A canary variable that is neither a secret nor on the allowlist is checked beside the
/// keys, because "the keys are gone" is what a denylist proves and "nothing else crossed" is what
/// an allowlist proves. The last test documents what the allowlist does NOT close.</para>
///
/// <para>Every test here spawns real child processes; the timeouts are hang nets, not budgets.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class ChildProcessEnvironmentTests : IDisposable
{
    /// <summary>
    /// Not a secret and on no list. If it crosses, the child's environment was inherited, not
    /// allowlisted — whatever happened to the keys.
    /// </summary>
    private const string CanaryName = "HYGIENE_ENV_CANARY";

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ashlar-child-env-" + Guid.NewGuid().ToString("N"));

    public ChildProcessEnvironmentTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A runner still winding down may hold a handle; the temp directory is not evidence.
        }
    }

    // ── the shared helper ──────────────────────────────────────────────────────────────────

    [Fact(Timeout = TestTimeouts.Quick)]
    public Task TheAllowlist_CopiesOnlyNamedVariables_AndRefusesEverythingUnderTheCertificationPrefix()
    {
        // The Ed25519 variable is deliberately NOT set here (nor anywhere in this class): other test
        // collections run concurrently and construct signers that resolve it from the environment,
        // and a placeholder that is not Base64 makes every one of them throw. Its coverage is the
        // prefix rule, asserted on the helper directly below.
        using var hmac = new EnvironmentVariableScope(CertificationRecordSigning.HmacKeyEnvVar, "operator-hmac");
        using var nugetConfig = new EnvironmentVariableScope("ASHLAR_CERT_NUGET_CONFIG", "/somewhere/NuGet.Config");
        using var canaryScope = new EnvironmentVariableScope(CanaryName, "canary");
        using var hook = new EnvironmentVariableScope("DOTNET_STARTUP_HOOKS", "/hostile/hook.dll");
        using var roll = new EnvironmentVariableScope("DOTNET_ROLL_FORWARD", "LatestMajor");

        var psi = new ProcessStartInfo("dotnet");
        ChildProcessEnvironment.Apply(psi);

        psi.Environment.Keys.Should().OnlyContain(name => ChildProcessEnvironment.IsAllowed(name),
            "every variable that crosses is one the list names");
        psi.Environment.Keys.Should().NotContain(name => name.StartsWith("ASHLAR_CERT_", StringComparison.OrdinalIgnoreCase));
        psi.Environment.Should().NotContainKey(CanaryName)
            .And.NotContainKey("DOTNET_STARTUP_HOOKS", "code injected into every process the runtime starts")
            .And.NotContainKey("DOTNET_ROLL_FORWARD", "the runner's own runtimeconfig decides its runtime");
        psi.Environment.Should().ContainKey("PATH", "the child still has to find things");

        ChildProcessEnvironment.Allowlist.Should().OnlyContain(
            name => !name.StartsWith(ChildProcessEnvironment.DeniedPrefix, StringComparison.OrdinalIgnoreCase),
            "no certification secret is ever on the list");
        ChildProcessEnvironment.IsAllowed(CertificationRecordSigning.HmacKeyEnvVar).Should().BeFalse();
        ChildProcessEnvironment.IsAllowed(CertificationRecordEd25519.PrivateKeyEnvVar).Should().BeFalse();
        ChildProcessEnvironment.IsAllowed("ashlar_cert_anything").Should().BeFalse("the prefix is refused in any case");
        ChildProcessEnvironment.IsAllowed("PATH").Should().BeTrue();
        return Task.CompletedTask;
    }

    // ── the author's build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// The build the loader runs over the author's project executes the author's targets. This
    /// project's target writes <c>env</c> (or <c>set</c>) to a file beside the project — what a
    /// hostile <c>&lt;Exec&gt;</c> would do with the key, minus the exfiltration.
    /// </summary>
    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task TheAuthorsBuild_SeesNeitherTheSigningKeysNorAnyOtherHostVariable()
    {
        var secret = "operator-hmac-" + Guid.NewGuid().ToString("N");
        var canary = "canary-" + Guid.NewGuid().ToString("N");
        // Only the HMAC key is set (see the helper test for why the Ed25519 variable is not).
        using var hmac = new EnvironmentVariableScope(CertificationRecordSigning.HmacKeyEnvVar, secret);
        using var canaryScope = new EnvironmentVariableScope(CanaryName, canary);
        // The plain restore a consumer gets, as every loader test that reaches a build assumes.
        using var plainRestore = EnvironmentVariableScope.Unset("ASHLAR_CERT_NUGET_CONFIG");

        var dump = Path.Combine(_dir, "env-dump.txt");
        File.WriteAllText(Path.Combine(_dir, "Brick.cs"), "public sealed class EnvDumpBrick { }");
        var csproj = Path.Combine(_dir, "EnvDump" + Guid.NewGuid().ToString("N")[..8] + ".csproj");
        File.WriteAllText(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <Target Name="DumpEnvironment" AfterTargets="Build">
                <Exec Condition="'$(OS)' != 'Windows_NT'" Command="env &gt; &quot;$(MSBuildProjectDirectory)/env-dump.txt&quot;" />
                <Exec Condition="'$(OS)' == 'Windows_NT'" Command="set &gt; &quot;$(MSBuildProjectDirectory)\env-dump.txt&quot;" />
              </Target>
            </Project>
            """);

        var outcome = await Task.Run(() => EvaluatedBrickProject.Build(csproj, Path.Combine(_dir, "out"), "Release", nugetConfigFile: null));

        outcome.ExitCode.Should().Be(0,
            "restore and build must still work under the allowlist — that is how the list was sized; MSBuild said: {0}", outcome.Output);
        File.Exists(dump).Should().BeTrue("the target ran, so the dump is the environment the author's build really had");
        var environment = File.ReadAllText(dump);
        environment.Should().ContainEquivalentOf("PATH=", "sanity: the dump is a real environment block");
        environment.Should().NotContain(secret, "the HMAC key's VALUE reached the author's build");
        environment.Should().NotContain(CertificationRecordSigning.HmacKeyEnvVar);
        environment.Should().NotContainEquivalentOf("ASHLAR_CERT_",
            "nothing under the certification prefix may cross, whatever its name");
        environment.Should().NotContain(canary,
            "a variable that is neither a secret nor on the allowlist crossed: the environment is inherited, not allowlisted");
        environment.Should().NotContain(CanaryName);
    }

    // ── the witness replay runner ──────────────────────────────────────────────────────────

    /// <summary>The runner replays the candidate; this candidate reports what it can see.</summary>
    private const string EnvironmentEchoBrickSource = """
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Hygiene;

public sealed class EnvironmentEchoBrick : Brick
{
    public EnvironmentEchoBrick()
    {
        Id = "environment-echo";
        Name = "environment-echo";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "reports the signing key and a canary as the runner sees them";
        Interface = new BrickInterface
        {
            Inputs = [ new BrickInputDefinition("value", "int", "value") ],
            Outputs =
            [
                new BrickOutputDefinition("hmacKey", "string", "ASHLAR_CERT_DEV_HMAC_KEY as the runner sees it"),
                new BrickOutputDefinition("canary", "string", "HYGIENE_ENV_CANARY as the runner sees it"),
                new BrickOutputDefinition("echo", "int", "echo")
            ]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var output = new BrickOutput();
        output.Set("hmacKey", Environment.GetEnvironmentVariable("ASHLAR_CERT_DEV_HMAC_KEY") ?? "<absent>");
        output.Set("canary", Environment.GetEnvironmentVariable("HYGIENE_ENV_CANARY") ?? "<absent>");
        output.Set("echo", input.Get<int>("value"));
        return Task.FromResult(output);
    }
}
""";

    [Fact(Timeout = TestTimeouts.Stress)]
    public async Task TheReplayRunner_SeesNeitherTheSigningKeyNorAnyOtherHostVariable()
    {
        var secret = "operator-hmac-" + Guid.NewGuid().ToString("N");
        var canary = "canary-" + Guid.NewGuid().ToString("N");
        using var hmac = new EnvironmentVariableScope(CertificationRecordSigning.HmacKeyEnvVar, secret);
        using var canaryScope = new EnvironmentVariableScope(CanaryName, canary);
        Environment.GetEnvironmentVariable(CertificationRecordSigning.HmacKeyEnvVar).Should().Be(secret,
            "sanity: the certifier's own process holds the key while the runner runs");

        const string typeName = "Hygiene.EnvironmentEchoBrick";
        var witness = new WitnessSpec("environment-echo", [Case(("value", 1), ("echo", 1))]);
        var request = Request(EnvironmentEchoBrickSource, typeName, witness, BrickReferences);
        using var backend = await LocalProcessExecutionBackend.CreateAsync(
            request, typeName, CandidateExecutionLimits.Default, CancellationToken.None);

        var report = await backend.ExecuteAsync(new CandidateExecutionJob(
            [new CandidateExecutionUnit("candidate", null, typeName)], witness, Repeats: 1));

        var observation = report.Observations.Should().ContainSingle().Subject;
        observation.Threw.Should().BeFalse("the candidate ran: {0}", observation.Error);
        observation.Outputs.Should().NotBeNull();
        observation.Outputs!["hmacKey"].Should().Be("<absent>",
            "the runner can read the key that signs its own certificate");
        observation.Outputs["canary"].Should().Be("<absent>",
            "a variable on no list crossed into the runner: the environment is inherited, not allowlisted");
    }

    // ── end to end: the forgery the two leaks enable ───────────────────────────────────────

    /// <summary>
    /// A candidate that does what the leak allows: mints an admitted record and signs it with
    /// whatever <c>ASHLAR_CERT_DEV_HMAC_KEY</c> the runner can see, resolved exactly as the
    /// certifier's own signer resolves it (no explicit key). Written to a path baked into the
    /// source, because the runner's scratch directory does not outlive the certification.
    /// </summary>
    private static string ForgingBrickSource(string forgedRecordPath) => $$"""
using System.IO;
using System.Text.Json;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Hygiene;

public sealed class ForgingBrick : Brick
{
    public ForgingBrick()
    {
        Id = "forger";
        Name = "forger";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "mints a certification record with whatever key the runner can see";
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
        var forged = new CertificationRecordData
        {
            Status = "PASS",
            Stage = "S0-S2",
            Admitted = true,
            Signed = true,
            Timestamp = new DateTimeOffset(2026, 9, 3, 0, 0, 0, TimeSpan.Zero),
            BrickId = "forger",
            ContentHash = "sha256-of-whatever-source-the-forger-wants-trusted",
            EscapeRate = 0,
            TotalMutants = 99,
            SurvivingMutants = 0,
            Gate = "Ashlar.Infrastructure.Certification.CertificationGate",
            SchemaVersion = CertificationRecordData.CurrentSchemaVersion,
        };
        // No explicit key: Sign resolves ASHLAR_CERT_DEV_HMAC_KEY from THIS process's environment,
        // exactly as the certifier's signer does. If the runner inherited it, this verifies under
        // the operator's key.
        forged = forged with { Signature = CertificationRecordSigning.Sign(forged) };
        File.WriteAllText(
            {{VerbatimLiteral(forgedRecordPath)}},
            JsonSerializer.Serialize(forged, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        var output = new BrickOutput();
        output.Set("echo", input.Get<int>("value"));
        return Task.FromResult(output);
    }
}
""";

    [Fact(Timeout = TestTimeouts.Stress)]
    public async Task ACandidateThatForgesARecordFromInsideTheRunner_GetsOneTheVerifierRejects()
    {
        var operatorKey = "operator-hmac-" + Guid.NewGuid().ToString("N");
        using var hmac = new EnvironmentVariableScope(CertificationRecordSigning.HmacKeyEnvVar, operatorKey);
        CertificationRecordSigning.UsesDevKey().Should().BeFalse(
            "sanity: the certifier signs with an operator key — the only key worth forging under");

        var forgedPath = Path.Combine(_dir, "forged-record.json");
        const string typeName = "Hygiene.ForgingBrick";
        // Wrong expectation on purpose: the forgery happens on the candidate's own witness run, and
        // a correctness rejection stops the gate right there — no mutation leg is needed for this fact.
        var witness = new WitnessSpec("forger", [Case(("value", 1), ("echo", 2))]);
        var references = new List<string>(BrickReferences)
        {
            typeof(CertificationRecordData).Assembly.Location,
            typeof(JsonSerializer).Assembly.Location,
        };
        var request = Request(ForgingBrickSource(forgedPath), typeName, witness, references);

        var decision = await new CertificationGate(new CertificationRecordSigner()).CertifyAsync(request);

        decision.Admitted.Should().BeFalse();
        decision.FailureCheck.Should().Be("correctness",
            "the candidate ran its witness case (and its forgery) and was rejected on the output: {0}", decision.Record.Reason);
        File.Exists(forgedPath).Should().BeTrue(
            "the candidate ran inside the runner and wrote its forgery; without that there is nothing to judge");
        var forged = JsonSerializer.Deserialize<CertificationRecordData>(
            File.ReadAllText(forgedPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        forged.Admitted.Should().BeTrue("the forgery claims admission");
        forged.Signature.Should().NotBeNullOrWhiteSpace("the forgery is signed");
        CertificationRecordSigning.VerifySignature(forged, operatorKey).Should().BeFalse(
            "the forged record verifies under the operator's key: the runner could read ASHLAR_CERT_DEV_HMAC_KEY and signed with it");
        CertificationRecordSigning.VerifySignature(forged, CertificationRecordSigning.DefaultDevKey).Should().BeTrue(
            "the runner saw no key and fell back to the committed dev key — the forgery is exactly as good as the public constant");
    }

    // ── what the allowlist does NOT close ──────────────────────────────────────────────────

    /// <summary>
    /// KNOWN GAP, documented rather than fixed here. The allowlist controls what a child is GIVEN;
    /// it cannot control what a child can READ. On Linux, <c>/proc/&lt;pid&gt;/environ</c> exposes
    /// a process's initial environment block to every process of the same uid — and the
    /// certifier's initial block is where a key exported from the launching shell lives. This test
    /// stands a parent up with a canary in its initial environment and has a child with an EMPTY
    /// environment read it back, which is the runner's position exactly.
    ///
    /// <para>What closes it: not holding the key in the environment at all (a key file readable
    /// only by the certifier's uid, or a signer the certifier calls and never sees the key of);
    /// running the children under a different uid or in their own PID namespace; or
    /// <c>hidepid=2</c> on <c>/proc</c>. If this test goes red, one of those landed on the platform
    /// running it — update "What the child processes see" in docs/CertificationGate.md and retire
    /// this test rather than weakening it.</para>
    /// </summary>
    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task KnownGap_OnLinux_ASameUidChildWithAnEmptyEnvironment_StillReadsItsParentsInitialEnvironmentThroughProc()
    {
        if (!OperatingSystem.IsLinux())
            return; // /proc is a Linux surface. (Windows exposes the same block via ReadProcessMemory on the PEB, likewise to the same user.)

        var canary = "canary-" + Guid.NewGuid().ToString("N");
        var psi = new ProcessStartInfo("/bin/sh")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        // Outer shell = the certifier: its INITIAL environment carries the canary. Inner shell = the
        // child: `env -i` gives it an environment emptier than any allowlist, and it reads the
        // parent's block by pid. The trailing `exit` keeps the outer shell from exec-ing the inner
        // command in its place (dash and bash both do that for a lone command), which would make
        // the inner shell's parent this test process instead.
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("env -i /bin/sh -c 'cat /proc/$PPID/environ'; exit $?");
        psi.Environment.Clear();
        psi.Environment["PATH"] = Environment.GetEnvironmentVariable("PATH") ?? "/usr/bin:/bin";
        psi.Environment[CanaryName] = canary;

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0, stderr);
        stdout.Should().Contain($"{CanaryName}={canary}",
            "KNOWN GAP: a same-uid child with an empty environment reads its parent's initial environment through /proc. "
            + "If this assertion fails, the platform closed it (hidepid, another uid, a PID namespace) — "
            + "update docs/CertificationGate.md and retire this test");
    }

    // ── helpers ────────────────────────────────────────────────────────────────────────────

    private static readonly IReadOnlyList<string> BrickReferences =
    [
        typeof(DomainBrick).Assembly.Location,
        typeof(BrickInput).Assembly.Location,
    ];

    private static WitnessCase Case((string Key, object Value) input, (string Key, object Value) expected) => new(
        new Dictionary<string, object> { [input.Key] = input.Value },
        new Dictionary<string, object> { [expected.Key] = expected.Value });

    /// <summary>A C# verbatim string literal for <paramref name="value"/>, to bake a path into brick source.</summary>
    private static string VerbatimLiteral(string value) => "@\"" + value.Replace("\"", "\"\"") + "\"";

    /// <summary>
    /// A byte-loaded brick (no on-disk artifact), so the runner replays the source it is given —
    /// the shape of a generated candidate.
    /// </summary>
    private static CertificationRequest Request(string source, string typeName, WitnessSpec witness, IReadOnlyList<string> references) => new()
    {
        Brick = InstantiateFromBytes(source, typeName, references),
        Witness = witness,
        SourceCode = source,
        ProjectPath = CreateCleanProjectFile(),
        CompilationReferences = references,
        BrickTypeName = typeName,
    };

    /// <summary>
    /// <c>CertifiedBrickCompiler.InstantiateBrick</c> compiles against the two brick assemblies only;
    /// the forging candidate also needs Ashlar.Certification.Contracts and System.Text.Json.
    /// </summary>
    private static DomainBrick InstantiateFromBytes(string source, string typeName, IReadOnlyList<string> references)
    {
        var assemblyName = $"HygieneBrick_{Guid.NewGuid():N}";
        var outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");
        var compile = new RoslynCodeAnalysisService(NullLogger<RoslynCodeAnalysisService>.Instance)
            .CompileAsync(CandidateSourceWrapper.Wrap(source), assemblyName, outputPath, references)
            .GetAwaiter().GetResult();
        compile.Success.Should().BeTrue("the fixture brick compiles: {0}", string.Join("; ", compile.Errors));

        var assembly = Assembly.Load(File.ReadAllBytes(compile.AssemblyPath!));
        return (DomainBrick)Activator.CreateInstance(assembly.GetType(typeName, throwOnError: true)!)!;
    }

    private static string CreateCleanProjectFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ashlar-cert-child-env-{Guid.NewGuid():N}.csproj");
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
