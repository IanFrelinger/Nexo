using System.Text;

namespace Nexo.Bricks.DepExtract;

/// <summary>Greppable provenance header prepended to every installed adapted_reader.hpp.</summary>
public static class AdapterProvenance
{
    public const string BeginMarker = "/* === NEXO_ADAPTER_PROVENANCE_BEGIN ===";
    public const string EndMarker = "=== NEXO_ADAPTER_PROVENANCE_END === */";

    public sealed record Info(
        string Strategy,           // deterministic|scaffold|model
        string? ModelTag,
        string? ModelDigest,
        bool? CompileGatePassed,
        string? OracleResult,      // pass|fail|n/a
        string ReviewedBy);        // human|oracle|none

    public static string Prepend(string adapterCode, Info info)
    {
        if (adapterCode.Contains("NEXO_ADAPTER_PROVENANCE_BEGIN", StringComparison.Ordinal))
            return adapterCode;

        var sb = new StringBuilder();
        sb.AppendLine(BeginMarker);
        sb.AppendLine(" * strategy: " + Sanitize(info.Strategy));
        sb.AppendLine(" * model_tag: " + Sanitize(info.ModelTag ?? "n/a"));
        sb.AppendLine(" * model_digest: " + Sanitize(info.ModelDigest ?? "n/a"));
        sb.AppendLine(" * compile_gate: " + (info.CompileGatePassed is null ? "n/a" : info.CompileGatePassed.Value ? "pass" : "fail"));
        sb.AppendLine(" * oracle: " + Sanitize(info.OracleResult ?? "n/a"));
        sb.AppendLine(" * reviewed_by: " + Sanitize(info.ReviewedBy));
        sb.AppendLine(" * installed_utc: " + DateTime.UtcNow.ToString("o"));
        sb.AppendLine(" " + EndMarker);
        sb.AppendLine();
        sb.Append(adapterCode);
        return sb.ToString();
    }

    /// <summary>Patch <c>oracle:</c> / <c>reviewed_by:</c> lines after post-install golden checks.</summary>
    public static string UpdateOracle(string adapterCode, string oracleResult, string reviewedBy)
    {
        if (!adapterCode.Contains("NEXO_ADAPTER_PROVENANCE_BEGIN", StringComparison.Ordinal))
            return adapterCode;

        var lines = adapterCode.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("* oracle:", StringComparison.Ordinal))
                lines[i] = " * oracle: " + Sanitize(oracleResult);
            else if (lines[i].Contains("* reviewed_by:", StringComparison.Ordinal))
                lines[i] = " * reviewed_by: " + Sanitize(reviewedBy);
        }
        return string.Join("\n", lines);
    }

    static string Sanitize(string s) =>
        s.Replace('\r', ' ').Replace('\n', ' ').Trim();
}
