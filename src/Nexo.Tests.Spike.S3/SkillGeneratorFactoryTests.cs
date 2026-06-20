using FluentAssertions;
using Nexo.Spike.S3.Generation;
using Xunit;

namespace Nexo.Tests.Spike.S3;

public sealed class SkillGeneratorFactoryTests
{
    [Fact]
    public void ResolveMode_defaults_to_recorded_when_env_unset()
    {
        using var scope = EnvScope.Unset("NEXO_S3_GENERATOR");
        SkillGeneratorFactory.ResolveMode().Should().Be(SkillGeneratorFactory.RecordedMode);
    }

    [Fact]
    public void Create_without_claude_gate_returns_recorded_generator()
    {
        using var scope = EnvScope.Unset("NEXO_S3_GENERATOR", "ANTHROPIC_API_KEY");
        var generator = SkillGeneratorFactory.Create();
        generator.BackendName.Should().Be(RecordedSkillGenerator.BackendLabel);
        generator.IsolationEnforced.Should().BeFalse();
    }

    [Fact]
    public void EnsureLiveModeConfigured_fails_fast_without_key_when_claude_gate_set()
    {
        using var scope = EnvScope.Set(
            ("NEXO_S3_GENERATOR", "claude"),
            ("ANTHROPIC_API_KEY", null));

        var act = () => SkillGeneratorFactory.EnsureLiveModeConfigured();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ANTHROPIC_API_KEY*")
            .Which.Message.Should().NotContain("sk-");
    }

    [Fact]
    public void EnsureLiveModeConfigured_does_not_require_key_for_recorded_backend()
    {
        using var scope = EnvScope.Unset("NEXO_S3_GENERATOR", "ANTHROPIC_API_KEY");
        var act = () => SkillGeneratorFactory.EnsureLiveModeConfigured();
        act.Should().NotThrow();
    }

    internal sealed class EnvScope : IDisposable
    {
        private readonly Dictionary<string, string?> _original = new(StringComparer.Ordinal);

        private EnvScope()
        {
        }

        public static EnvScope Unset(params string[] keys) => Set(keys.Select(k => (k, (string?)null)).ToArray());

        public static EnvScope Set(params (string Key, string? Value)[] entries)
        {
            var scope = new EnvScope();
            foreach (var (key, value) in entries)
            {
                scope._original[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }

            return scope;
        }

        public void Dispose()
        {
            foreach (var (key, value) in _original)
                Environment.SetEnvironmentVariable(key, value);
        }
    }
}
