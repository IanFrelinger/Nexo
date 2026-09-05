using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ashlar.Abstractions.Routing;
using Ashlar.BackgroundAgents.Registry;
using Ashlar.BackgroundAgents.Trust;
using Ashlar.Cloud;
using Ashlar.Cluster;
using Ashlar.Contracts.Distributed;
using Ashlar.Core.Application.Configuration.Ports;
using Ashlar.Hosting;
using Ashlar.Infrastructure.Execution;
using Ashlar.Mcp.Client;
using Ashlar.Mcp.Server;
using Ashlar.Native;
using Ashlar.Runtime.Routing;
using Ashlar.Transport.A2A;
using Ashlar.Transport.A2A.Server;
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

        var again = await scheduler.ScheduleAsync(envelope);
        again.TaskId.Should().Be(handle.TaskId);

        var evidence = await scheduler.GetResultAsync(handle.TaskId);
        evidence.Should().NotBeNull();
        evidence!.Status.Should().Be(ResultEvidenceStatus.Succeeded);
        evidence.OutputHash.Should().Be("sha256:payload");
        evidence.EnvelopeId.Should().Be("env-1");

        var canceled = () => scheduler.ScheduleAsync(envelope, new CancellationToken(canceled: true));
        await canceled.Should().ThrowAsync<OperationCanceledException>();

        var differentHash = envelope with { PayloadHash = "sha256:other" };
        var conflict = () => scheduler.ScheduleAsync(differentHash);
        await conflict.Should().ThrowAsync<InvalidOperationException>().WithMessage("*different payload hash*");

        var concurrent = new InMemoryTaskScheduler();
        var handles = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => concurrent.ScheduleAsync(envelope)));
        handles.Select(h => h.TaskId).Distinct().Should().ContainSingle();
        (await concurrent.GetResultAsync(handles[0].TaskId))!.OutputHash.Should().Be("sha256:payload");
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

        var oop = await host.ExecuteAsync(
            NativeArtifactManifest.Create("art-o", NativeArtifactFormat.OutOfProcessWorker, "sha256:wasm", "main"),
            envelope);
        oop.Status.Should().Be(ResultEvidenceStatus.Succeeded);

        var canceled = () => host.ExecuteAsync(
            NativeArtifactManifest.Create("art-c", NativeArtifactFormat.WebAssembly, "sha256:wasm", "_start"),
            envelope,
            new CancellationToken(canceled: true));
        await canceled.Should().ThrowAsync<OperationCanceledException>();

        var nullManifest = () => host.ExecuteAsync(null!, envelope);
        await nullManifest.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Workstation_locks_secure_profile_and_trust_after_caller_configure()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAshlarWorkstation(options =>
        {
            options.TrustEnabled = false;
            options.DeploymentProfile = AshlarDeploymentProfile.Full;
        });
        using var sp = services.BuildServiceProvider(validateScopes: true);

        sp.GetRequiredService<ICloudSanitizationProxy>().Should().NotBeNull();
        sp.GetRequiredService<IProviderFactory>().Should().BeOfType<SanitizingProviderFactory>();
        sp.GetService<IGrpcChannelFactory>().Should().BeNull();
    }

    [Fact]
    public void Workstation_composition_refuses_remote_protocols_without_env_var()
    {
        var prev = Environment.GetEnvironmentVariable("ASHLAR_DEPLOYMENT_PROFILE");
        Environment.SetEnvironmentVariable("ASHLAR_DEPLOYMENT_PROFILE", null);
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAshlarWorkstation();
            using var sp = services.BuildServiceProvider(validateScopes: true);
            sp.GetService<IGrpcChannelFactory>().Should().BeNull();

            var client = new AshlarMcpClientOptions { Enabled = true };
            client.Servers.Add(new McpServerEndpointOptions
            {
                Name = "github",
                Url = "https://mcp.example.com/mcp",
            });
            new ValidateAshlarMcpClientOptions().Validate(null, client).Failed.Should().BeTrue();

            new ValidateA2ATransportOptions()
                .Validate(null, new A2ATransportOptions { Enabled = true })
                .Failed.Should().BeTrue();

            new ValidateAshlarA2AServerOptions()
                .Validate(null, new AshlarA2AServerOptions
                {
                    Enabled = true,
                    PublicBaseUrl = "https://peer.example.com",
                    DefaultExecutionTimeout = TimeSpan.FromSeconds(30),
                })
                .Failed.Should().BeTrue();

            new ValidateAshlarMcpServerOptions()
                .Validate(null, new AshlarMcpServerOptions { Enabled = true, ServerName = "ashlar-ide" })
                .Succeeded.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASHLAR_DEPLOYMENT_PROFILE", prev);
            AshlarDeploymentProfileEnvironment.ClearResolved();
        }
    }

    [Fact]
    public void AirGapped_composition_refuses_mcp_server_without_env_var()
    {
        var prev = Environment.GetEnvironmentVariable("ASHLAR_DEPLOYMENT_PROFILE");
        Environment.SetEnvironmentVariable("ASHLAR_DEPLOYMENT_PROFILE", null);
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAshlarProfile(AshlarDeploymentProfile.AirGapped);
            using var sp = services.BuildServiceProvider(validateScopes: true);

            new ValidateAshlarMcpServerOptions()
                .Validate(null, new AshlarMcpServerOptions { Enabled = true, ServerName = "ashlar-ide" })
                .Failed.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASHLAR_DEPLOYMENT_PROFILE", prev);
            AshlarDeploymentProfileEnvironment.ClearResolved();
        }
    }

    [Fact]
    public void Cloud_directory_rejects_blank_and_non_positive_records()
    {
        var blankOrg = () => Organization.Create(" ", "Northwind");
        blankOrg.Should().Throw<ArgumentException>();

        var blankName = () => Organization.Create("org-1", " ");
        blankName.Should().Throw<ArgumentException>();

        var zeroQuota = () => OrganizationQuota.Create("org-1", 0, null);
        zeroQuota.Should().Throw<ArgumentOutOfRangeException>();

        var negativeBudget = () => OrganizationQuota.Create("org-1", 1, -1);
        negativeBudget.Should().Throw<ArgumentOutOfRangeException>();

        var blankPlan = () => BillingAccount.Create("org-1", " ");
        blankPlan.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Cloud_project_has_no_kernel_project_reference()
    {
        var repo = FindRepoRoot();
        var cloudCsproj = Path.Combine(repo, "products", "ashlar-cloud", "src", "Ashlar.Cloud", "Ashlar.Cloud.csproj");
        File.Exists(cloudCsproj).Should().BeTrue(cloudCsproj);
        File.ReadAllText(cloudCsproj).Should().NotContain("ProjectReference");
    }

    [Fact]
    public void Product_projects_do_not_reference_commercial_or_kernel_from_cloud()
    {
        var repo = FindRepoRoot();
        var productsRoot = Path.Combine(repo, "products");
        foreach (var csproj in Directory.GetFiles(productsRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(csproj);
            text.Should().NotContain("commercial/", because: csproj);

            var rel = Path.GetRelativePath(repo, csproj).Replace('\\', '/');
            if (rel.StartsWith("products/ashlar-cloud/", StringComparison.Ordinal))
            {
                text.Should().NotContain("ProjectReference", because: csproj);
            }
        }

        foreach (var source in Directory.GetFiles(
                     Path.Combine(productsRoot, "ashlar-cloud"), "*.cs", SearchOption.AllDirectories))
        {
            var kernelImports = File.ReadAllLines(source)
                .Select(static line => line.TrimStart())
                .Where(static line =>
                    line.StartsWith("using Ashlar.", StringComparison.Ordinal) &&
                    !line.StartsWith("using Ashlar.Cloud", StringComparison.Ordinal))
                .ToArray();
            kernelImports.Should().BeEmpty(because: source);
        }
    }

    private static string FindRepoRoot([CallerFilePath] string? sourceFile = null)
    {
        foreach (var start in RepoRootStarts(sourceFile))
        {
            var dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Ashlar.sln")) ||
                    File.Exists(Path.Combine(dir.FullName, "products", "Ashlar.Products.sln")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }
        }

        throw new InvalidOperationException(
            "Could not locate Ashlar.sln from " + AppContext.BaseDirectory +
            " cwd=" + Directory.GetCurrentDirectory() +
            " source=" + sourceFile);
    }

    private static IEnumerable<string> RepoRootStarts(string? sourceFile)
    {
        if (!string.IsNullOrWhiteSpace(sourceFile) && File.Exists(sourceFile))
        {
            var fromSource = Path.GetDirectoryName(sourceFile);
            if (!string.IsNullOrWhiteSpace(fromSource))
            {
                yield return fromSource;
            }
        }

        yield return AppContext.BaseDirectory;
        yield return Directory.GetCurrentDirectory();
    }
}
