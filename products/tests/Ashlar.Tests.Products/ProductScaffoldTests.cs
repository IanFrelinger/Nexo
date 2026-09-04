using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Ashlar.Abstractions.Routing;
using Ashlar.BackgroundAgents.Registry;
using Ashlar.BackgroundAgents.Trust;
using Ashlar.Cloud;
using Ashlar.Cluster;
using Ashlar.Contracts.Distributed;
using Ashlar.Core.Application.Configuration.Ports;
using Ashlar.Hosting;
using Ashlar.Native;
using Ashlar.Runtime.Routing;
using Ashlar.Transport.Grpc;
using Ashlar.Workstation;
using Xunit;

namespace Ashlar.Tests.Products;

public sealed class ProductScaffoldTests
{
    [Fact]
    public void Workstation_composes_secure_workstation_with_trust_and_without_grpc()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAshlarWorkstation();
        using var sp = services.BuildServiceProvider(validateScopes: true);

        sp.GetRequiredService<IConfigurationService>().Should().NotBeNull();
        sp.GetRequiredService<IBackgroundAgentRegistry>().Should().NotBeNull();
        sp.GetRequiredService<ICloudSanitizationProxy>().Should().NotBeNull();
        sp.GetService<IGrpcChannelFactory>().Should().BeNull();
        sp.GetRequiredService<IEndpointRegistry>().Should().NotBeOfType<InMemoryEndpointRegistry>();
    }

    [Fact]
    public async Task Cluster_scheduler_returns_evidence_for_scheduled_envelope()
    {
        var scheduler = new InMemoryTaskScheduler();
        var envelope = ExecutionEnvelope.Create(
            "env-1",
            "edge-a",
            ExecutionTarget.Cluster,
            "brick.execute",
            "sha256:payload",
            "policy-pack-1",
            DateTimeOffset.Parse("2026-09-04T00:00:00Z"));

        var handle = await scheduler.ScheduleAsync(envelope);
        handle.TaskId.Should().Be("task:env-1");
        handle.EnvelopeId.Should().Be("env-1");

        var evidence = await scheduler.GetResultAsync(handle.TaskId);
        evidence.Should().NotBeNull();
        evidence!.Status.Should().Be(ResultEvidenceStatus.Succeeded);
        evidence.OutputHash.Should().Be("sha256:payload");
        evidence.EnvelopeId.Should().Be("env-1");
    }

    [Fact]
    public void Cloud_directory_stores_org_quota_and_billing_without_kernel_types()
    {
        IOrganizationDirectory directory = new InMemoryOrganizationDirectory();
        directory.Upsert(
            new Organization("org-1", "Northwind"),
            new OrganizationQuota("org-1", MaxConcurrentTasks: 4, MaxMonthlyTokenBudget: 1_000),
            new BillingAccount("org-1", "team"));

        directory.GetOrganization("org-1")!.DisplayName.Should().Be("Northwind");
        directory.GetQuota("org-1")!.MaxConcurrentTasks.Should().Be(4);
        directory.GetBilling("org-1")!.PlanId.Should().Be("team");
        directory.GetOrganization("missing").Should().BeNull();
    }

    [Fact]
    public void Cloud_directory_rejects_mismatched_ids()
    {
        var directory = new InMemoryOrganizationDirectory();
        var act = () => directory.Upsert(
            new Organization("org-1", "Northwind"),
            new OrganizationQuota("org-2", 1, null),
            new BillingAccount("org-1", "team"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Native_host_accepts_wasm_when_hashes_match()
    {
        INativeExecutionHost host = new WasmNativeExecutionHost();
        host.Supports(NativeArtifactFormat.WebAssembly).Should().BeTrue();
        host.Supports(NativeArtifactFormat.OutOfProcessWorker).Should().BeTrue();
        host.Supports(NativeArtifactFormat.ManagedAssembly).Should().BeFalse();

        var manifest = NativeArtifactManifest.Create(
            "art-1",
            NativeArtifactFormat.WebAssembly,
            "sha256:wasm",
            "_start");
        var envelope = ExecutionEnvelope.Create(
            "env-n",
            "ws-1",
            ExecutionTarget.Local,
            "native.execute",
            "sha256:wasm",
            "policy-pack-1",
            DateTimeOffset.Parse("2026-09-04T00:00:00Z"));

        var evidence = await host.ExecuteAsync(manifest, envelope);
        evidence.Status.Should().Be(ResultEvidenceStatus.Succeeded);
        evidence.OutputHash.Should().Be("sha256:wasm");
    }

    [Fact]
    public async Task Native_host_rejects_managed_assembly_and_hash_mismatch()
    {
        var host = new WasmNativeExecutionHost();
        var envelope = ExecutionEnvelope.Create(
            "env-n",
            "ws-1",
            ExecutionTarget.Local,
            "native.execute",
            "sha256:wasm",
            "policy-pack-1",
            DateTimeOffset.Parse("2026-09-04T00:00:00Z"));

        var managed = await host.ExecuteAsync(
            NativeArtifactManifest.Create("art-m", NativeArtifactFormat.ManagedAssembly, "sha256:wasm", "Main"),
            envelope);
        managed.Status.Should().Be(ResultEvidenceStatus.Rejected);
        managed.Detail.Should().Contain("WebAssembly");

        var mismatch = await host.ExecuteAsync(
            NativeArtifactManifest.Create("art-w", NativeArtifactFormat.WebAssembly, "sha256:other", "_start"),
            envelope);
        mismatch.Status.Should().Be(ResultEvidenceStatus.Rejected);
        mismatch.Detail.Should().Contain("hash");
    }

    [Fact]
    public void Cloud_project_has_no_kernel_project_reference()
    {
        var cloudCsproj = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "ashlar-cloud", "src", "Ashlar.Cloud", "Ashlar.Cloud.csproj"));
        File.Exists(cloudCsproj).Should().BeTrue(cloudCsproj);
        File.ReadAllText(cloudCsproj).Should().NotContain("ProjectReference");
    }
}
