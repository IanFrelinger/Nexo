using System.IO;
using System.Text.Json;
using Nexo.Abstractions;

namespace Nexo.Policies.Dev;

/// <summary>
/// Development policy that restricts file operations to allowlisted paths.
/// 
/// Only allows file writes and search/replace operations in:
/// - src/ directory
/// - tests/ directory
/// 
/// Prevents modifications to other parts of the codebase.
/// Implements IPolicy for use with PolicyEngine.
/// </summary>
public sealed class PathAllowlist : IPolicy
{
    private static readonly string[] DefaultAllowed =
    {
        "src/",
        "tests/",
        "docs/",
        ".nexo/"
    };

    private readonly string[] _allowedPrefixes;

    public PathAllowlist(IEnumerable<string>? extraAllowedPrefixes = null)
    {
        var merged = new List<string>(DefaultAllowed);

        if (extraAllowedPrefixes is not null)
            merged.AddRange(extraAllowedPrefixes);

        var fromEnv = Environment.GetEnvironmentVariable("NEXO_PATH_ALLOWLIST_EXTRA");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            foreach (var entry in fromEnv.Split(',', ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                merged.Add(entry);
        }

        _allowedPrefixes = merged
            .Select(NormalizePrefix)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()!;
    }

    public bool Approve(ToolCall call, WorldSnapshot s, out string reason)
    {
        reason = "OK";
        if (call.Id is "repo.fs.write" or "repo.fs.search_replace")
        {
            if (call.Arguments.ValueKind == JsonValueKind.Object &&
                call.Arguments.TryGetProperty("path", out var p))
            {
                var raw = (p.ValueKind == JsonValueKind.Null ? "" : (p.GetString() ?? "")).Replace('\\', '/');
                var rel = raw.TrimStart('/');
                if (string.IsNullOrEmpty(rel))
                {
                    reason = "Path not allowed: empty or null path";
                    return false;
                }
                if (Path.IsPathRooted(raw) || raw.StartsWith("/", StringComparison.Ordinal))
                {
                    var sandboxRoot = ResolveSandboxRoot(s);
                    if (string.IsNullOrWhiteSpace(sandboxRoot))
                    {
                        reason = $"Path not allowed: absolute path not permitted: {raw}";
                        return false;
                    }

                    string fullPath;
                    try
                    {
                        fullPath = Path.GetFullPath(raw);
                    }
                    catch
                    {
                        reason = $"Path not allowed: invalid absolute path: {raw}";
                        return false;
                    }

                    if (!IsPathWithinRoot(fullPath, sandboxRoot))
                    {
                        reason = $"Path not allowed: outside SandboxRoot: {fullPath}";
                        return false;
                    }

                    return true;
                }
                if (rel.Contains("..", StringComparison.Ordinal))
                {
                    reason = $"Path not allowed: path traversal not permitted: {rel}";
                    return false;
                }
                if (!_allowedPrefixes.Any(a => rel.StartsWith(a, StringComparison.OrdinalIgnoreCase)))
                {
                    reason = $"Path not allowed: {rel}";
                    return false;
                }
            }
        }
        return true;
    }

    private static string? NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return null;

        var normalized = prefix.Replace('\\', '/').Trim();
        normalized = normalized.TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        if (normalized.Contains("..", StringComparison.Ordinal))
            return null;

        if (!normalized.EndsWith("/", StringComparison.Ordinal))
            normalized += "/";

        return normalized;
    }

    private static string? ResolveSandboxRoot(WorldSnapshot snapshot)
    {
        string? root = null;
        if (snapshot.Data.TryGetValue("SandboxRoot", out var rootObj) && rootObj is string fromSnapshot)
            root = fromSnapshot;

        if (string.IsNullOrWhiteSpace(root))
            root = Environment.GetEnvironmentVariable("NEXO_SANDBOX_ROOT");

        if (string.IsNullOrWhiteSpace(root))
            return null;

        try
        {
            return Path.GetFullPath(root);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsPathWithinRoot(string candidate, string root)
    {
        var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedCandidate = candidate;

        if (normalizedCandidate.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            return true;

        var prefix = normalizedRoot + Path.DirectorySeparatorChar;
        return normalizedCandidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
