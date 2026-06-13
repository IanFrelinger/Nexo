namespace Nexo.CLI.Commands;

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
        var normalized = (qaPolicy ?? "auto").Trim().ToLowerInvariant();
        return normalized switch
        {
            "demo" => "demo",
            "release" => "release",
            "prod" => "prod",
            "research" => "research",
            _ => "auto"
        };
    }


    internal static string NormalizeBenchmarkSet(string? benchmarkSet)
    {
        var normalized = (benchmarkSet ?? "adhoc").Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "adhoc" : normalized;
    }


}
