using FluentAssertions;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Domain;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Networking;
using Nexo.Infrastructure.Pipelines;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Configuration;

/// <summary>
/// Golden tests for <see cref="NexoDefaults"/>. These lock down the default values
/// so accidental changes are caught immediately. If a default changes intentionally,
/// update both the constant and this test.
/// </summary>
[Trait("Category", "E2E")]
public sealed class NexoDefaultsTests
{
    [Fact(Timeout = TestTimeouts.Quick)]
    public void LlmDefaults_AreStable()
    {
        NexoDefaults.LlmRetryCount.Should().Be(3);
        NexoDefaults.LlmTemperature.Should().Be(0.2);
        NexoDefaults.LlmMaxTokens.Should().Be(4096);
        NexoDefaults.MockDelayMs.Should().Be(30);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void OpenAiDefaults_AreStable()
    {
        NexoDefaults.OpenAiDefaultModel.Should().Be("gpt-4o-mini");
        NexoDefaults.OpenAiDefaultBaseUrl.Should().Be("https://api.openai.com/v1/chat/completions");
        NexoDefaults.OpenAiDefaultVisionModel.Should().Be("gpt-4o-mini");
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void AzureDefaults_AreStable()
    {
        NexoDefaults.AzureOpenAiDefaultApiVersion.Should().Be("2024-06-01");
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void OllamaDefaults_AreStable()
    {
        NexoDefaults.OllamaDefaultBaseUrl.Should().Be("http://localhost:11434");
        NexoDefaults.OllamaDefaultModel.Should().Be("llama3.1");
        NexoDefaults.OllamaDefaultVisionModel.Should().Be("richardyoung/smolvlm2-2.2b-instruct");
        NexoDefaults.OllamaDefaultTimeoutSeconds.Should().Be(300);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void PipelineDefaults_AreStable()
    {
        NexoDefaults.PipelineMaxRetryAttempts.Should().Be(3);
        NexoDefaults.PipelineRetryDelayMs.Should().Be(100);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void ConfigDefaults_AreStable()
    {
        NexoDefaults.AnalysisMaxComplexityThreshold.Should().Be(20);
        NexoDefaults.ValidationTimeoutSeconds.Should().Be(300);
        NexoDefaults.ConfigFileName.Should().Be("config.json");
        NexoDefaults.ConfigDirectoryName.Should().Be(".nexo");
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void AuditDefaults_AreStable()
    {
        NexoDefaults.SanitizationAuditMaxEntries.Should().Be(10_000);
        NexoDefaults.DataDecisionAuditMaxEntries.Should().Be(50_000);
        NexoDefaults.AgentLogMaxEntriesPerAgent.Should().Be(1_000);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void RunPodDefaults_AreStable()
    {
        NexoDefaults.RunPodDefaultBaseUrl.Should().Be("https://api.runpod.io");
        NexoDefaults.RunPodDefaultGpuTier.Should().Be("NVIDIA_A4000");
        NexoDefaults.RunPodDefaultTimeoutMinutes.Should().Be(10);
        NexoDefaults.RunPodDefaultQueueDepthThreshold.Should().Be(4);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void NetworkingDefaults_AreStable()
    {
        NexoDefaults.NetworkBusHeartbeatIntervalSeconds.Should().Be(30);
        NexoDefaults.NetworkBusMaxEventHistory.Should().Be(10_000);
        NexoDefaults.NetworkBusDefaultMaxHops.Should().Be(3);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void PipelineExecutionOptions_UsesNexoDefaults()
    {
        var opts = new PipelineExecutionOptions();
        opts.MaxRetryAttempts.Should().Be(NexoDefaults.PipelineMaxRetryAttempts);
        opts.RetryDelayMs.Should().Be(NexoDefaults.PipelineRetryDelayMs);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void RunPodBrickConfig_UsesNexoDefaults()
    {
        var cfg = new RunPodBrickConfig();
        cfg.BaseUrl.Should().Be(NexoDefaults.RunPodDefaultBaseUrl);
        cfg.PreferredGpuTier.Should().Be(NexoDefaults.RunPodDefaultGpuTier);
        cfg.QueueDepthThreshold.Should().Be(NexoDefaults.RunPodDefaultQueueDepthThreshold);
        cfg.PeerCapabilityId.Should().Be(NexoDefaults.RunPodDefaultPeerCapabilityId);
        cfg.PeerRoutingBrickId.Should().Be(NexoDefaults.RunPodDefaultPeerRoutingBrickId);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void NetworkBusOptions_UsesNexoDefaults()
    {
        var opts = new NetworkBusOptions();
        opts.HeartbeatIntervalSeconds.Should().Be(NexoDefaults.NetworkBusHeartbeatIntervalSeconds);
        opts.MaxEventHistory.Should().Be(NexoDefaults.NetworkBusMaxEventHistory);
        opts.DefaultMaxHops.Should().Be(NexoDefaults.NetworkBusDefaultMaxHops);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void BrickUsageTrackerOptions_UsesNexoDefaults()
    {
        var opts = new BrickUsageTrackerOptions();
        opts.MaxEntries.Should().Be(NexoDefaults.BrickUsageTrackerMaxEntries);
        opts.RollingHourWindowSeconds.Should().Be(NexoDefaults.BrickUsageTrackerRollingHourWindowSeconds);
    }
}
