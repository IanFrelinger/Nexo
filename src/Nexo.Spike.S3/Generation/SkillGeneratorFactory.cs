namespace Nexo.Spike.S3.Generation;

public static class SkillGeneratorFactory
{
    public const string RecordedMode = "recorded";
    public const string ClaudeMode = "claude";

    /// <summary>Legacy alias kept for S3.0 compatibility in reports/tests.</summary>
    public const string ScriptedStandInMode = "scripted-standin";

    public static string ResolveMode()
    {
        var configured = Environment.GetEnvironmentVariable("NEXO_S3_GENERATOR")?.Trim();
        if (string.IsNullOrWhiteSpace(configured))
            return RecordedMode;

        return configured.ToLowerInvariant() switch
        {
            RecordedMode or ScriptedStandInMode => RecordedMode,
            ClaudeMode => ClaudeMode,
            _ => throw new InvalidOperationException(
                $"Unknown NEXO_S3_GENERATOR value '{configured}'. Use '{RecordedMode}' or '{ClaudeMode}'.")
        };
    }

    public static ISkillGenerator Create(string? workRoot = null) =>
        ResolveMode() switch
        {
            ClaudeMode => new Claude.ClaudeSkillGenerator(workRoot: workRoot),
            _ => new RecordedSkillGenerator()
        };

    public static void EnsureLiveModeConfigured()
    {
        if (!string.Equals(ResolveMode(), ClaudeMode, StringComparison.OrdinalIgnoreCase))
            return;

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
        {
            throw new InvalidOperationException(
                "NEXO_S3_GENERATOR=claude requires ANTHROPIC_API_KEY at call time. " +
                "The key is read from the environment only and is never persisted.");
        }
    }
}
