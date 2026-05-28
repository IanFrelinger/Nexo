using FluentAssertions;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Application.Fleet.Models;
using Nexo.Core.Application.Mesh;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Pipelines.Models;
using Xunit;

namespace Nexo.Tests.Application;

public sealed class ApplicationRemainingCoverageTests
{
    [Fact]
    public void Result_success_and_failure_paths()
    {
        var ok = Result<string>.Success("payload");
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailure.Should().BeFalse();
        ok.Value.Should().Be("payload");
        ok.Error.Should().BeNull();

        var fail = Result<int>.Failure("E1", "bad", "detail");
        fail.IsSuccess.Should().BeFalse();
        fail.IsFailure.Should().BeTrue();
        fail.Value.Should().Be(default);
        fail.Error!.Code.Should().Be("E1");
        fail.Error.Message.Should().Be("bad");
        fail.Error.Detail.Should().Be("detail");
    }

    [Fact]
    public void Unit_value_is_default_struct()
    {
        Unit.Value.Should().Be(default(Unit));
    }

    [Fact]
    public void MeshTrustPolicyConfiguration_normalizes_and_resolves_env()
    {
        MeshTrustPolicyConfiguration.NormalizePolicy("TRUSTED-ONLY").Should().Be("trusted-only");
        MeshTrustPolicyConfiguration.NormalizePolicy("allowlist").Should().Be("allowlist");
        MeshTrustPolicyConfiguration.NormalizePolicy("bogus").Should().Be("trusted-preferred");

        var mesh = Environment.GetEnvironmentVariable("NEXO_MESH_TRUST_POLICY");
        var peer = Environment.GetEnvironmentVariable("NEXO_PEER_TRUST_POLICY");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_MESH_TRUST_POLICY", "any");
            Environment.SetEnvironmentVariable("NEXO_PEER_TRUST_POLICY", null);
            MeshTrustPolicyConfiguration.ResolveDiscoveryPolicy().Should().Be("any");

            Environment.SetEnvironmentVariable("NEXO_MESH_TRUST_POLICY", null);
            Environment.SetEnvironmentVariable("NEXO_PEER_TRUST_POLICY", "trusted-only");
            MeshTrustPolicyConfiguration.ResolveCapabilityRequestPolicy().Should().Be("trusted-only");

            Environment.SetEnvironmentVariable("NEXO_MESH_TRUST_POLICY", null);
            Environment.SetEnvironmentVariable("NEXO_PEER_TRUST_POLICY", null);
            MeshTrustPolicyConfiguration.ResolveDiscoveryPolicy().Should().Be("any");
            MeshTrustPolicyConfiguration.ResolveCapabilityRequestPolicy().Should().Be("trusted-preferred");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_MESH_TRUST_POLICY", mesh);
            Environment.SetEnvironmentVariable("NEXO_PEER_TRUST_POLICY", peer);
        }
    }

    [Fact]
    public void InstanceCapabilities_dedupes_formats_and_exposes_static_profiles()
    {
        var caps = new InstanceCapabilities(
            new[] { ArtifactFormat.Source, ArtifactFormat.Source, ArtifactFormat.Binary },
            ArtifactFormat.Binary,
            canCompile: true,
            hasDockerRuntime: true,
            hasWasmRuntime: false,
            isAirGapped: true,
            availableComponents: new[] { "nexo-cli" });

        caps.SupportedFormats.Should().HaveCount(2);
        caps.PreferredFormat.Should().Be(ArtifactFormat.Binary);
        caps.CanCompile.Should().BeTrue();
        caps.HasDockerRuntime.Should().BeTrue();
        caps.IsAirGapped.Should().BeTrue();
        caps.AvailableComponents.Should().ContainSingle();

        InstanceCapabilities.AllFormats.SupportedFormats.Should().HaveCount(3);
        InstanceCapabilities.LocalNexo.CanCompile.Should().BeTrue();
        InstanceCapabilities.LocalNexo.AvailableComponents.Should().Contain("nexo-cli");
    }

    [Fact]
    public void MeshTaskState_record_holds_scheduling_metadata()
    {
        var state = new MeshTaskState(
            TaskId: "t1",
            Name: "render",
            Steps: 2,
            RequiredBrickIds: new[] { "b1" },
            Affinity: new Dictionary<string, string> { ["gpu"] = "a100" },
            Priority: 5,
            DeadlineUtc: DateTimeOffset.UtcNow.AddHours(1),
            Status: MeshTaskStatus.Pending,
            AssignedPeerId: null,
            AssignedApiBaseUrl: null,
            PlacementReason: null,
            AttemptCount: 0,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            LastScheduledAtUtc: null,
            CorrelationId: "corr",
            IdempotencyKey: "idem",
            LastScheduleIdempotencyKey: null,
            ResultSummary: null,
            ResultHandle: null,
            LeaseToken: null,
            LeaseOwnerPeerId: null,
            LeaseExpiresUtc: null,
            CheckpointHandle: null);

        state.TaskId.Should().Be("t1");
        state.Affinity["gpu"].Should().Be("a100");
        state.CorrelationId.Should().Be("corr");
    }

    [Fact]
    public void Pipeline_definition_records_expose_constraints_and_bindings()
    {
        var constraints = new PipelineStageConstraints
        {
            MaxParallelism = 4,
            ForceSerialized = true,
            Critical = true,
            RequiresPostValidation = true,
        };
        constraints.MaxParallelism.Should().Be(4);
        constraints.Critical.Should().BeTrue();

        var binding = new PipelineWorkerBinding
        {
            DeterministicPoolId = "det-pool",
            AgenticPoolId = "ag-pool",
        };
        binding.AgenticPoolId.Should().Be("ag-pool");

        var stage = new PipelineStageDefinition
        {
            Id = "s1",
            Name = "Analyze",
            StageType = PipelineStageType.FanOut,
            Mode = PipelineExecutionMode.Hybrid,
            JoinStrategy = PipelineJoinStrategy.Quorum,
            FallbackChain = new[] { PipelineWorkerType.Agentic, PipelineWorkerType.Deterministic },
            Constraints = constraints,
            WorkerBinding = binding,
        };
        stage.JoinStrategy.Should().Be(PipelineJoinStrategy.Quorum);
        stage.WorkerBinding.DeterministicPoolId.Should().Be("det-pool");
    }

    [Fact]
    public void RunPod_routing_enums_and_job_status_terminal_flag()
    {
        GpuComputeClass.High.Should().Be(GpuComputeClass.High);
        RemoteExecutionPreference.CloudOnly.Should().Be(RemoteExecutionPreference.CloudOnly);
        RunPodJobState.Unknown.Should().Be(RunPodJobState.Unknown);

        new JobStatus { State = RunPodJobState.Completed }.IsTerminal.Should().BeTrue();
        new JobStatus { State = RunPodJobState.Running }.IsTerminal.Should().BeFalse();
    }
}
