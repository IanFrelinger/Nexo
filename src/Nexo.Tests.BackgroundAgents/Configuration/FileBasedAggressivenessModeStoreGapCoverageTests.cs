using FluentAssertions;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Configuration;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Configuration;

public sealed class FileBasedAggressivenessModeStoreGapCoverageTests
{
    [Fact]
    public void GetMode_missing_file_returns_passive()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-mode-missing-" + Guid.NewGuid().ToString("N") + ".json");
        var store = new FileBasedAggressivenessModeStore(path);

        store.GetMode().Should().Be(BackgroundAgentAggressivenessMode.Passive);
    }

    [Fact]
    public void GetMode_corrupt_json_returns_passive()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-mode-corrupt-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, "{not-json");
        try
        {
            var store = new FileBasedAggressivenessModeStore(path);
            store.GetMode().Should().Be(BackgroundAgentAggressivenessMode.Passive);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(BackgroundAgentAggressivenessMode.Passive, "passive")]
    [InlineData(BackgroundAgentAggressivenessMode.SemiActive, "semi-active")]
    [InlineData(BackgroundAgentAggressivenessMode.Active, "active")]
    [InlineData(BackgroundAgentAggressivenessMode.Ambient, "ambient")]
    public void SetMode_persists_all_modes(BackgroundAgentAggressivenessMode mode, string expectedJsonMode)
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-mode-roundtrip-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var store = new FileBasedAggressivenessModeStore(path);
            store.SetMode(mode);

            File.ReadAllText(path).Should().Contain(expectedJsonMode);
            store.GetMode().Should().Be(mode);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetMode_accepts_semiactive_alias_without_hyphen()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-mode-semi-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, """{"Mode":"semiactive"}""");
        try
        {
            var store = new FileBasedAggressivenessModeStore(path);
            store.GetMode().Should().Be(BackgroundAgentAggressivenessMode.SemiActive);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetMode_unknown_value_defaults_to_active()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-mode-unknown-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, """{"Mode":"turbo"}""");
        try
        {
            var store = new FileBasedAggressivenessModeStore(path);
            store.GetMode().Should().Be(BackgroundAgentAggressivenessMode.Active);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
