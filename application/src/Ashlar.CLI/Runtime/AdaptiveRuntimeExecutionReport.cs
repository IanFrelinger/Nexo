using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ashlar.CLI.Runtime;

/// <summary>Append-only adaptive runtime execution report persisted for operator history.</summary>
public sealed record AdaptiveRuntimeExecutionReport
{
    /// <summary>Run id.</summary>
    public string RunId { get; init; } = Guid.NewGuid().ToString("N");
    /// <summary>Started at utc.</summary>
    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    /// <summary>Elapsed ms.</summary>
    public long ElapsedMs { get; init; }
    /// <summary>Goal fingerprint.</summary>
    public string GoalFingerprint { get; init; } = string.Empty;
    /// <summary>Goal preview.</summary>
    public string GoalPreview { get; init; } = string.Empty;
    /// <summary>Benchmark set.</summary>
    public string BenchmarkSet { get; init; } = "adhoc";
    /// <summary>Requested qa policy.</summary>
    public string RequestedQaPolicy { get; init; } = "auto";
    /// <summary>Resolved qa policy.</summary>
    public string ResolvedQaPolicy { get; init; } = "demo";
    /// <summary>Bootstrap profile.</summary>
    public string BootstrapProfile { get; init; } = "self-extend-functional";
    /// <summary>Run visual qa.</summary>
    public bool RunVisualQa { get; init; }
    /// <summary>Visual qa fallback policy.</summary>
    public string VisualQaFallbackPolicy { get; init; } = "degrade";
    /// <summary>Success.</summary>
    public bool Success { get; init; }
    /// <summary>Failure stage.</summary>
    public string FailureStage { get; init; } = "none";
    /// <summary>Bootstrap ok.</summary>
    public bool BootstrapOk { get; init; }
    /// <summary>Preflight ran.</summary>
    public bool PreflightRan { get; init; }
    /// <summary>Preflight ok.</summary>
    public bool PreflightOk { get; init; }
    /// <summary>Self extend ran.</summary>
    public bool SelfExtendRan { get; init; }
    /// <summary>Self extend ok.</summary>
    public bool SelfExtendOk { get; init; }
    /// <summary>Plan reasons.</summary>
    public string[] PlanReasons { get; init; } = Array.Empty<string>();
    /// <summary>Adaptive policy reason.</summary>
    public string? AdaptivePolicyReason { get; init; }

    /// <summary>Computes a stable SHA-256 fingerprint for a normalized goal string.</summary>
    public static string ComputeGoalFingerprint(string goal)
    {
        var normalized = NormalizeGoal(goal);
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Builds a truncated preview of the normalized goal for display and storage.</summary>
    public static string BuildGoalPreview(string goal, int maxLength = 160)
    {
        var normalized = NormalizeGoal(goal);
        if (normalized.Length <= maxLength)
            return normalized;
        return normalized[..maxLength] + "...";
    }

    private static string NormalizeGoal(string goal)
    {
        var trimmed = (goal ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(trimmed))
            return string.Empty;

        var collapsed = string.Join(
            ' ',
            trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return collapsed.Length <= 2000 ? collapsed : collapsed[..2000];
    }
}
