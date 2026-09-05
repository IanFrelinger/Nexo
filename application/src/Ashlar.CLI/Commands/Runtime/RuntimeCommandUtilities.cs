namespace Ashlar.CLI.Commands.Runtime;
/// <summary>Runtime command utilities.</summary>
internal static class RuntimeCommandUtilities
{
    internal static string ReadEnvString(string key, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }


    internal static int ReadEnvInt(string key, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (int.TryParse(value, out var parsed))
            return parsed;
        return fallback;
    }


    internal static double ReadEnvDouble(string key, double fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return fallback;
    }


    internal static string NormalizeQaPolicy(string? qaPolicy)
    {
        return TryNormalizeQaPolicy(qaPolicy, out var normalized) ? normalized : "auto";
    }

    internal static bool TryNormalizeQaPolicy(string? qaPolicy, out string normalized)
    {
        if (string.IsNullOrWhiteSpace(qaPolicy))
        {
            normalized = "auto";
            return true;
        }

        normalized = qaPolicy.Trim().ToLowerInvariant();
        return normalized is "auto" or "demo" or "release" or "prod" or "research";
    }

    internal static bool TryNormalizeBootstrapProfile(string? profile, out string normalized)
    {
        if (string.IsNullOrWhiteSpace(profile))
        {
            normalized = "auto";
            return true;
        }

        normalized = profile.Trim().ToLowerInvariant();
        return normalized is "auto" or "self-extend-functional" or "self-extend-aesthetic" or "self-extend-visual";
    }


    internal static string NormalizeBenchmarkSet(string? benchmarkSet)
    {
        var normalized = (benchmarkSet ?? "adhoc").Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "adhoc" : normalized;
    }


    internal const string InvalidMaxIterationsMessage = "Invalid --max-iterations. Use a positive integer.";

    internal const string InvalidLimitMessage = "Invalid --limit. Use a positive integer.";

    internal const string InvalidHistoryWindowMessage = "Invalid --history-window. Use a positive integer.";

    internal const string InvalidMinTotalMessage = "Invalid --min-total. Use a positive integer.";

    internal const string InvalidCoreMinTotalMessage = "Invalid --core-min-total. Use a positive integer.";

    internal const string InvalidVisualMinTotalMessage = "Invalid --visual-min-total. Use a positive integer.";

    internal const string InvalidLaneRepetitionsMessage = "Invalid --lane-repetitions. Use a positive integer.";

    internal const string InvalidCoreHistoryWindowMessage = "Invalid --core-history-window. Use a positive integer.";

    internal const string InvalidVisualHistoryWindowMessage = "Invalid --visual-history-window. Use a positive integer.";

    internal const string InvalidMinPassRateMessage = "Invalid --min-pass-rate. Use a number in [0,1].";

    internal const string InvalidCoreMinPassRateMessage = "Invalid --core-min-pass-rate. Use a number in [0,1].";

    internal const string InvalidVisualMinPassRateMessage = "Invalid --visual-min-pass-rate. Use a number in [0,1].";

    internal const string InvalidMinConsecutivePassesMessage = "Invalid --min-consecutive-passes. Use a non-negative integer.";

    internal const string InvalidVisualPromotionStreakMessage = "Invalid --visual-promotion-streak. Use a non-negative integer.";

    internal const string InvalidNcrFailureRateSloMessage = "Invalid --ncr-failure-rate-slo. Use a number in [0,1].";

    internal const string InvalidNcrResolutionMsSloMessage = "Invalid --ncr-resolution-ms-slo. Use a positive number.";

    internal const string InvalidNcrLoadMsSloMessage = "Invalid --ncr-load-ms-slo. Use a positive number.";

    internal const string InvalidNcrOutcomeMsSloMessage = "Invalid --ncr-outcome-ms-slo. Use a positive number.";

    internal const string InvalidMaxRemediationAttemptsMessage = "Invalid --max-remediation-attempts. Use a non-negative integer.";


    internal static bool TryValidateMaxIterationsOverride(int? maxIterations)
        => !maxIterations.HasValue || maxIterations.Value > 0;


    internal static bool TryValidatePositiveCount(int value)
        => value > 0;

    internal static bool TryValidateNonNegativeCount(int value)
        => value >= 0;

    internal static bool TryValidateUnitInterval(double value)
        => double.IsFinite(value) && value is >= 0d and <= 1d;

    internal static bool TryValidatePositiveDuration(double value)
        => double.IsFinite(value) && value > 0d;


}
