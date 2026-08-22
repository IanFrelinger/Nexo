using FluentAssertions;
using Ashlar.Core.Application.Execution.Routing;
using Ashlar.Core.Domain;
using Ashlar.Infrastructure.Pipelines;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Configuration;

/// <summary>
/// Golden tests for <see cref="AshlarDefaults"/>. These lock down the default values
/// so accidental changes are caught immediately. If a default changes intentionally,
/// update both the constant and this test.
/// </summary>
[Trait("Category", "E2E")]
public sealed class AshlarDefaultsTests
{
    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task LlmDefaults_AreStable()
    {
        await Task.CompletedTask;
        AshlarDefaults.LlmRetryCount.Should().Be(3);
        AshlarDefaults.LlmTemperature.Should().Be(0.2);
        AshlarDefaults.LlmMaxTokens.Should().Be(4096);
        AshlarDefaults.MockDelayMs.Should().Be(30);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task OpenAiDefaults_AreStable()
    {
        await Task.CompletedTask;
        AshlarDefaults.OpenAiDefaultModel.Should().Be("gpt-4o-mini");
        AshlarDefaults.OpenAiDefaultBaseUrl.Should().Be("https://api.openai.com/v1/chat/completions");
        AshlarDefaults.OpenAiDefaultVisionModel.Should().Be("gpt-4o-mini");
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task AzureDefaults_AreStable()
    {
        await Task.CompletedTask;
        AshlarDefaults.AzureOpenAiDefaultApiVersion.Should().Be("2024-06-01");
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task OllamaDefaults_AreStable()
    {
        await Task.CompletedTask;
        AshlarDefaults.OllamaDefaultBaseUrl.Should().Be("http://localhost:11434");
        AshlarDefaults.OllamaDefaultModel.Should().Be("llama3.1:latest");
        AshlarDefaults.OllamaDefaultVisionModel.Should().Be("richardyoung/smolvlm2-2.2b-instruct");
        AshlarDefaults.OllamaDefaultTimeoutSeconds.Should().Be(300);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task PipelineDefaults_AreStable()
    {
        await Task.CompletedTask;
        AshlarDefaults.PipelineMaxRetryAttempts.Should().Be(3);
        AshlarDefaults.PipelineRetryDelayMs.Should().Be(100);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task ConfigDefaults_AreStable()
    {
        await Task.CompletedTask;
        AshlarDefaults.AnalysisMaxComplexityThreshold.Should().Be(20);
        AshlarDefaults.ValidationTimeoutSeconds.Should().Be(300);
        AshlarDefaults.ConfigFileName.Should().Be("config.json");
        AshlarDefaults.ConfigDirectoryName.Should().Be(".ashlar");
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task AuditDefaults_AreStable()
    {
        await Task.CompletedTask;
        AshlarDefaults.SanitizationAuditMaxEntries.Should().Be(10_000);
        AshlarDefaults.DataDecisionAuditMaxEntries.Should().Be(50_000);
        AshlarDefaults.AgentLogMaxEntriesPerAgent.Should().Be(1_000);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task RunPodDefaults_AreStable()
    {
        await Task.CompletedTask;
        AshlarDefaults.RunPodDefaultBaseUrl.Should().Be("https://api.runpod.io");
        AshlarDefaults.RunPodDefaultGpuTier.Should().Be("NVIDIA_A4000");
        AshlarDefaults.RunPodDefaultTimeoutMinutes.Should().Be(10);
        AshlarDefaults.RunPodDefaultQueueDepthThreshold.Should().Be(4);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task NetworkingDefaults_AreStable()
    {
        await Task.CompletedTask;
        AshlarDefaults.NetworkBusHeartbeatIntervalSeconds.Should().Be(30);
        AshlarDefaults.NetworkBusMaxEventHistory.Should().Be(10_000);
        AshlarDefaults.NetworkBusDefaultMaxHops.Should().Be(3);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task PipelineExecutionOptions_UsesAshlarDefaults()
    {
        await Task.CompletedTask;
        var opts = new PipelineExecutionOptions();
        opts.MaxRetryAttempts.Should().Be(AshlarDefaults.PipelineMaxRetryAttempts);
        opts.RetryDelayMs.Should().Be(AshlarDefaults.PipelineRetryDelayMs);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task RunPodBrickConfig_UsesAshlarDefaults()
    {
        await Task.CompletedTask;
        var cfg = new RunPodBrickConfig();
        cfg.BaseUrl.Should().Be(AshlarDefaults.RunPodDefaultBaseUrl);
        cfg.PreferredGpuTier.Should().Be(AshlarDefaults.RunPodDefaultGpuTier);
        cfg.QueueDepthThreshold.Should().Be(AshlarDefaults.RunPodDefaultQueueDepthThreshold);
        cfg.PeerCapabilityId.Should().Be(AshlarDefaults.RunPodDefaultPeerCapabilityId);
        cfg.PeerRoutingBrickId.Should().Be(AshlarDefaults.RunPodDefaultPeerRoutingBrickId);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task VideoDefaults_AreStable()
    {
        await Task.CompletedTask;
        AshlarDefaults.VideoDefaultFps.Should().Be(5);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public async Task EmbeddingDefaults_AreStable()
    {
        await Task.CompletedTask;
        AshlarDefaults.EmbeddingDefaultDimension.Should().Be(64);
    }
}
