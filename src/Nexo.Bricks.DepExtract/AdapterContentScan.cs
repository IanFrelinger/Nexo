using System.Text.RegularExpressions;

namespace Nexo.Bricks.DepExtract;

/// <summary>
/// Pre-compile / pre-install scan for disallowed constructs in model-authored C++.
/// Override only with <c>NEXO_ALLOW_UNSAFE_ADAPTER=1</c> and <c>NEXO_UNSAFE_ADAPTER_REASON</c>.
/// </summary>
public static class AdapterContentScan
{
    public const string AllowUnsafeEnv = "NEXO_ALLOW_UNSAFE_ADAPTER";
    public const string UnsafeReasonEnv = "NEXO_UNSAFE_ADAPTER_REASON";

    static readonly string[] AllowedIncludeLeaves =
    [
        "adapted_reader.hpp", "processing.hpp", "json.hpp", "reader.hpp",
        "cstdint", "cstddef", "cstring", "string", "vector", "array", "span",
        "optional", "variant", "memory", "utility", "algorithm", "iterator",
        "fstream", "sstream", "iostream", "filesystem", "chrono", "cmath",
        "stdexcept", "type_traits", "limits", "map", "unordered_map", "set",
        "deque", "list", "tuple", "functional", "regex", "iomanip",
    ];

    static readonly (string Name, Regex Rx)[] Patterns =
    [
        ("system(", new Regex(@"\bsystem\s*\(", RegexOptions.Compiled)),
        ("exec*", new Regex(@"\bexec[vlpe]*\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("popen", new Regex(@"\bpopen\s*\(", RegexOptions.Compiled)),
        ("fork", new Regex(@"\bfork\s*\(", RegexOptions.Compiled)),
        ("socket", new Regex(@"\bsocket\s*\(", RegexOptions.Compiled)),
        ("connect", new Regex(@"\bconnect\s*\(", RegexOptions.Compiled)),
        ("absolute fopen", new Regex(@"\bfopen\s*\(\s*""/", RegexOptions.Compiled)),
        ("absolute ofstream", new Regex(@"\bofstream\s*[^\(;]*\(\s*""/", RegexOptions.Compiled)),
        ("#include <cstdlib> system path", new Regex(@"#\s*include\s*<cstdlib>", RegexOptions.Compiled)),
    ];

    public sealed record Hit(string Rule, string Excerpt);

    public sealed record Result(bool Ok, IReadOnlyList<Hit> Hits, bool OverrideUsed, string? OverrideReason);

    public static Result Scan(string adapterCode, bool isModelAuthored)
    {
        if (!isModelAuthored)
            return new Result(true, Array.Empty<Hit>(), false, null);

        var hits = new List<Hit>();
        foreach (var (name, rx) in Patterns)
        {
            var m = rx.Match(adapterCode);
            if (m.Success)
                hits.Add(new Hit(name, Excerpt(adapterCode, m.Index)));
        }

        foreach (Match m in Regex.Matches(adapterCode, @"#\s*include\s*[<""]([^>""]+)[>""]"))
        {
            var inc = m.Groups[1].Value.Replace('\\', '/');
            var leaf = Path.GetFileName(inc);
            if (inc.StartsWith('/') || Regex.IsMatch(inc, @"^[A-Za-z]:"))
            {
                hits.Add(new Hit("absolute include", Excerpt(adapterCode, m.Index)));
                continue;
            }
            // Allow relative project headers and allowlisted std/Poco leaves.
            if (inc.Contains('/') && !inc.StartsWith("Qt", StringComparison.Ordinal)
                && !AllowedIncludeLeaves.Contains(leaf, StringComparer.OrdinalIgnoreCase)
                && !leaf.EndsWith(".hpp", StringComparison.OrdinalIgnoreCase)
                && !leaf.EndsWith(".h", StringComparison.OrdinalIgnoreCase))
            {
                hits.Add(new Hit($"non-allowlisted include:{inc}", Excerpt(adapterCode, m.Index)));
            }
        }

        if (hits.Count == 0)
            return new Result(true, hits, false, null);

        if (string.Equals(Environment.GetEnvironmentVariable(AllowUnsafeEnv), "1", StringComparison.Ordinal))
        {
            var reason = Environment.GetEnvironmentVariable(UnsafeReasonEnv);
            if (string.IsNullOrWhiteSpace(reason))
            {
                return new Result(false, hits, false,
                    "NEXO_ALLOW_UNSAFE_ADAPTER=1 requires NEXO_UNSAFE_ADAPTER_REASON to be set");
            }
            return new Result(true, hits, true, reason);
        }

        return new Result(false, hits, false, null);
    }

    static string Excerpt(string code, int index)
    {
        var start = Math.Max(0, index - 20);
        var len = Math.Min(80, code.Length - start);
        return code.Substring(start, len).Replace('\n', ' ').Replace('\r', ' ');
    }
}
