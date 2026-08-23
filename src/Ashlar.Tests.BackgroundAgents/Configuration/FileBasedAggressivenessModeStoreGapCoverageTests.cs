using FluentAssertions;
using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Configuration;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Configuration;

/// <summary>Tests for file based aggressiveness mode store gap coverage.</summary>
public sealed class FileBasedAggressivenessModeStoreGapCoverageTests
{
    [Fact]
    public void GetMode_missing_file_returns_passive()
    {
        var path = Path.Combine(Path.GetTempPath(), "ashlar-mode-missing-" + Guid.NewGuid().ToString("N") + ".json");
        var store = new FileBasedAggressivenessModeStore(path);

        store.GetMode().Should().Be(BackgroundAgentAggressivenessMode.Passive);
    }

    [Fact]
    public void GetMode_corrupt_json_returns_passive()
    {
        var path = Path.Combine(Path.GetTempPath(), "ashlar-mode-corrupt-" + Guid.NewGuid().ToString("N") + ".json");
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
        var path = Path.Combine(Path.GetTempPath(), "ashlar-mode-roundtrip-" + Guid.NewGuid().ToString("N") + ".json");
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
        var path = Path.Combine(Path.GetTempPath(), "ashlar-mode-semi-" + Guid.NewGuid().ToString("N") + ".json");
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
    public void GetMode_unknown_value_fails_closed_to_passive_and_warns()
    {
        // A typo must never arm the extender: 'turbo' is not a mode, so the effective mode
        // is Passive (observe only) and the operator is told why in the log.
        var path = Path.Combine(Path.GetTempPath(), "ashlar-mode-unknown-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, """{"Mode":"turbo"}""");
        try
        {
            var logger = new ListLogger<FileBasedAggressivenessModeStore>();
            var store = new FileBasedAggressivenessModeStore(path, logger);

            store.GetMode().Should().Be(BackgroundAgentAggressivenessMode.Passive);

            logger.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.Message.Contains("turbo") && e.Message.Contains("Passive"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetMode_empty_document_fails_closed_to_passive_and_warns()
    {
        // '{}' used to read as Active because the DTO defaulted its Mode to "active" — a
        // file with no decision in it must not be the file that arms the loop.
        var path = Path.Combine(Path.GetTempPath(), "ashlar-mode-empty-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, "{}");
        try
        {
            var logger = new ListLogger<FileBasedAggressivenessModeStore>();
            var store = new FileBasedAggressivenessModeStore(path, logger);

            store.GetMode().Should().Be(BackgroundAgentAggressivenessMode.Passive);

            logger.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.Message.Contains("no 'Mode' value"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetMode_logs_the_effective_mode_once_per_change_not_per_read()
    {
        var path = Path.Combine(Path.GetTempPath(), "ashlar-mode-log-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var logger = new ListLogger<FileBasedAggressivenessModeStore>();
            var store = new FileBasedAggressivenessModeStore(path, logger);

            store.GetMode().Should().Be(BackgroundAgentAggressivenessMode.Passive, "missing file");
            store.GetMode();
            store.SetMode(BackgroundAgentAggressivenessMode.Active);
            store.GetMode().Should().Be(BackgroundAgentAggressivenessMode.Active);
            store.GetMode();

            var effective = logger.Entries.Where(e => e.Level == LogLevel.Information && e.Message.Contains("aggressiveness mode:")).ToList();
            effective.Should().HaveCount(2, "Passive once, then Active once — a per-cycle read must not spam the log");
            effective[0].Message.Should().Contain("Passive");
            effective[1].Message.Should().Contain("Active");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
        private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
    }
}
