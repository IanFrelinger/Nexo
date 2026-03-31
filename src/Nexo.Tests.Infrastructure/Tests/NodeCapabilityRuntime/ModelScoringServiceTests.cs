using FluentAssertions;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Infrastructure.NodeCapabilityRuntime.Policies;
using Nexo.Infrastructure.NodeCapabilityRuntime.Scoring;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

public sealed class ModelScoringServiceTests
{
    [Fact]
    public void ScoreModel_ReturnsMinValue_WhenCapabilityMissing()
    {
        var policy = new LinuxPolicy();
        var sut = new ModelScoringService(policy);
        var model = CreateModel(capabilities: new[] { TaskCapability.Embeddings });
        var context = new TaskContext { RequiredCapability = TaskCapability.CodeGeneration };
        var profile = CreateProfile();

        var score = sut.ScoreModel(model, context, profile);

        score.Should().Be(float.MinValue);
    }

    [Fact]
    public void ScoreModel_PrefersHotModel_AndFastPathForInteractive()
    {
        var policy = new LinuxPolicy();
        var sut = new ModelScoringService(policy);
        var hot = CreateModel(id: "hot", state: ModelState.Hot, quality: 0.7f, speed: 0.8f);
        var cold = CreateModel(id: "cold", state: ModelState.Cold, quality: 0.7f, speed: 0.8f);
        var context = new TaskContext
        {
            RequiredCapability = TaskCapability.TextGeneration,
            Latency = LatencyRequirement.Interactive
        };
        var profile = CreateProfile();

        var hotScore = sut.ScoreModel(hot, context, profile);
        var coldScore = sut.ScoreModel(cold, context, profile);

        hotScore.Should().BeGreaterThan(coldScore);
    }

    [Fact]
    public void ScoreModel_AppliesBatteryPenalty_ForLargeModelOnBattery()
    {
        var policy = new LinuxPolicy();
        var sut = new ModelScoringService(policy);
        var model = CreateModel(weightSizeBytes: 8L * 1024 * 1024 * 1024);
        var context = new TaskContext { RequiredCapability = TaskCapability.TextGeneration };
        var profilePlugged = CreateProfile(isOnBattery: false, isCharging: false);
        var profileBattery = CreateProfile(isOnBattery: true, isCharging: false);

        var pluggedScore = sut.ScoreModel(model, context, profilePlugged);
        var batteryScore = sut.ScoreModel(model, context, profileBattery);

        batteryScore.Should().BeLessThan(pluggedScore);
    }

    private static ModelDescriptor CreateModel(
        string id = "m1",
        ModelState state = ModelState.Hot,
        float quality = 0.75f,
        float speed = 0.75f,
        long weightSizeBytes = 2L * 1024 * 1024 * 1024,
        IEnumerable<TaskCapability>? capabilities = null)
    {
        return new ModelDescriptor
        {
            Id = id,
            State = state,
            QualityScore = quality,
            SpeedScore = speed,
            WeightSizeBytes = weightSizeBytes,
            MinRAMRequiredBytes = 1024L * 1024 * 1024,
            Capabilities = new HashSet<TaskCapability>(capabilities ?? new[] { TaskCapability.TextGeneration }),
            SupportedPlatforms = new HashSet<PlatformType> { PlatformType.Linux }
        };
    }

    private static NodeProfile CreateProfile(bool isOnBattery = false, bool isCharging = true)
    {
        return new NodeProfile
        {
            Platform = PlatformType.Linux,
            AvailableRAMBytes = 16L * 1024 * 1024 * 1024,
            TotalRAMBytes = 32L * 1024 * 1024 * 1024,
            AvailableVRAMBytes = 10L * 1024 * 1024 * 1024,
            TotalVRAMBytes = 12L * 1024 * 1024 * 1024,
            IsOnBattery = isOnBattery,
            IsCharging = isCharging
        };
    }
}
