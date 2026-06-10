using FluentAssertions;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Domain;
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
    public async Task LlmDefaults_AreStable()
    {
        await Task.CompletedTask;
        NexoDefaults.LlmRetryCount.Should().Be(3);
        NexoDefaults.LlmTemperature.Should().Be(0.2);
        NexoDefaults.LlmMaxTokens.Should().Be(4096);
        NexoDefaults.MockDelayMs.Should().Be(30);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task OpenAiDefaults_AreStable()
    {
        await Task.CompletedTask;
        NexoDefaults.OpenAiDefaultModel.Should().Be("gpt-4o-mini");
        NexoDefaults.OpenAiDefaultBaseUrl.Should().Be("https://api.openai.com/v1/chat/completions");
        NexoDefaults.OpenAiDefaultVisionModel.Should().Be("gpt-4o-mini");
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task AzureDefaults_AreStable()
    {
        await Task.CompletedTask;
        NexoDefaults.AzureOpenAiDefaultApiVersion.Should().Be("2024-06-01");
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task OllamaDefaults_AreStable()
    {
        await Task.CompletedTask;
        NexoDefaults.OllamaDefaultBaseUrl.Should().Be("http://localhost:11434");
        NexoDefaults.OllamaDefaultModel.Should().Be("llama3.1:latest");
        NexoDefaults.OllamaDefaultVisionModel.Should().Be("richardyoung/smolvlm2-2.2b-instruct");
        NexoDefaults.OllamaDefaultTimeoutSeconds.Should().Be(300);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task PipelineDefaults_AreStable()
    {
        await Task.CompletedTask;
        NexoDefaults.PipelineMaxRetryAttempts.Should().Be(3);
        NexoDefaults.PipelineRetryDelayMs.Should().Be(100);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task ConfigDefaults_AreStable()
    {
        await Task.CompletedTask;
        NexoDefaults.AnalysisMaxComplexityThreshold.Should().Be(20);
        NexoDefaults.ValidationTimeoutSeconds.Should().Be(300);
        NexoDefaults.ConfigFileName.Should().Be("config.json");
        NexoDefaults.ConfigDirectoryName.Should().Be(".nexo");
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task AuditDefaults_AreStable()
    {
        await Task.CompletedTask;
        NexoDefaults.SanitizationAuditMaxEntries.Should().Be(10_000);
        NexoDefaults.DataDecisionAuditMaxEntries.Should().Be(50_000);
        NexoDefaults.AgentLogMaxEntriesPerAgent.Should().Be(1_000);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task RunPodDefaults_AreStable()
    {
        await Task.CompletedTask;
        NexoDefaults.RunPodDefaultBaseUrl.Should().Be("https://api.runpod.io");
        NexoDefaults.RunPodDefaultGpuTier.Should().Be("NVIDIA_A4000");
        NexoDefaults.RunPodDefaultTimeoutMinutes.Should().Be(10);
        NexoDefaults.RunPodDefaultQueueDepthThreshold.Should().Be(4);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task NetworkingDefaults_AreStable()
    {
        await Task.CompletedTask;
        NexoDefaults.NetworkBusHeartbeatIntervalSeconds.Should().Be(30);
        NexoDefaults.NetworkBusMaxEventHistory.Should().Be(10_000);
        NexoDefaults.NetworkBusDefaultMaxHops.Should().Be(3);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task PipelineExecutionOptions_UsesNexoDefaults()
    {
        await Task.CompletedTask;
        var opts = new PipelineExecutionOptions();
        opts.MaxRetryAttempts.Should().Be(NexoDefaults.PipelineMaxRetryAttempts);
        opts.RetryDelayMs.Should().Be(NexoDefaults.PipelineRetryDelayMs);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task RunPodBrickConfig_UsesNexoDefaults()
    {
        await Task.CompletedTask;
        var cfg = new RunPodBrickConfig();
        cfg.BaseUrl.Should().Be(NexoDefaults.RunPodDefaultBaseUrl);
        cfg.PreferredGpuTier.Should().Be(NexoDefaults.RunPodDefaultGpuTier);
        cfg.QueueDepthThreshold.Should().Be(NexoDefaults.RunPodDefaultQueueDepthThreshold);
        cfg.PeerCapabilityId.Should().Be(NexoDefaults.RunPodDefaultPeerCapabilityId);
        cfg.PeerRoutingBrickId.Should().Be(NexoDefaults.RunPodDefaultPeerRoutingBrickId);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task VideoDefaults_AreStable()
    {
        await Task.CompletedTask;
        NexoDefaults.VideoDefaultFps.Should().Be(5);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task EmbeddingDefaults_AreStable()
    {
        await Task.CompletedTask;
        NexoDefaults.EmbeddingDefaultDimension.Should().Be(64);
    }
}
