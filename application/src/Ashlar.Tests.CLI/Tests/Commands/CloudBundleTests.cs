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
        deploy.Should().Contain("az acr build").And.Contain("az container create").And.Contain("--restart-policy Never");
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
