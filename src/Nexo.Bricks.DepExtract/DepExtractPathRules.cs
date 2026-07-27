namespace Nexo.Bricks.DepExtract;

/// <summary>Shared path safety checks for brick inputs (traversal, absolute entries, missing files).</summary>
public static class DepExtractPathRules
{
    public static string? TryGetWorkspaceRoot()
    {
        var raw = Environment.GetEnvironmentVariable("WORKSPACE_ROOT");
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try { return Path.GetFullPath(raw.Trim()); }
        catch { return null; }
    }

    public static string RequireExistingDirectory(string? path, string paramName)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw DepExtractException.InvalidInput($"{paramName} is required.");
        var full = Path.GetFullPath(path);
        if (!Directory.Exists(full))
            throw DepExtractException.NotFound($"{paramName} not found: {path}");
        return full;
    }

    /// <summary>
    /// When WORKSPACE_ROOT is set (compose/agent), <paramref name="path"/> must resolve under it.
    /// No-op when WORKSPACE_ROOT is unset (unit tests / local non-compose runs).
    /// </summary>
    public static string EnsureUnderWorkspaceRootIfConfigured(string path, string paramName)
    {
        var full = Path.GetFullPath(path);
        var root = TryGetWorkspaceRoot();
        if (root is null) return full;

        if (!IsUnderRoot(root, full))
        {
            throw DepExtractException.InvalidInput(
                $"{paramName} must be under WORKSPACE_ROOT ({root}) so docker.sock bind-mounts resolve. Got: {full}");
        }
        return full;
    }

    /// <summary>
    /// Poco tree may sit next to the transfer bundle (outside WORKSPACE_ROOT) when
    /// compose also bind-mounts POCO_CONTEXT. Allow WORKSPACE_ROOT or configured POCO_CONTEXT.
    /// </summary>
    public static string EnsurePocoContextAllowed(string path, string paramName = "pocoContext")
    {
        var full = Path.GetFullPath(path);
        var workspace = TryGetWorkspaceRoot();
        if (workspace is null) return full;
        if (IsUnderRoot(workspace, full)) return full;

        var pocoEnv = Environment.GetEnvironmentVariable("POCO_CONTEXT");
        if (!string.IsNullOrWhiteSpace(pocoEnv))
        {
            try
            {
                var pocoRoot = Path.GetFullPath(pocoEnv.Trim());
                if (IsUnderRoot(pocoRoot, full) || full.Equals(pocoRoot, StringComparison.OrdinalIgnoreCase))
                    return full;
            }
            catch
            {
                // fall through to error
            }
        }

        throw DepExtractException.InvalidInput(
            $"{paramName} must be under WORKSPACE_ROOT ({workspace}) or the configured POCO_CONTEXT so docker.sock bind-mounts resolve. Got: {full}");
    }

    public static bool IsUnderRoot(string rootDir, string candidatePath)
    {
        var rootFull = Path.GetFullPath(rootDir)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(candidatePath);
        return full.Equals(Path.GetFullPath(rootDir), StringComparison.OrdinalIgnoreCase)
            || full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Ensures <paramref name="relative"/> stays under <paramref name="rootDir"/> and optionally that the file exists.</summary>
    public static string ResolveUnderRoot(string rootDir, string relative, string paramName, bool requireFile = false, bool requireDir = false)
    {
        if (string.IsNullOrWhiteSpace(relative))
            throw DepExtractException.InvalidInput($"{paramName} must not be empty.");
        if (Path.IsPathRooted(relative))
            throw DepExtractException.InvalidInput($"{paramName} must be relative to the source root, not absolute: {relative}");
        if (relative.Contains('\\'))
            relative = relative.Replace('\\', '/');
        if (relative.Split('/').Any(p => p is ".." or ""))
        {
            // allow "." as a single segment for scanDir, but reject ".." and empty segments from "//"
            var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Any(p => p == ".."))
                throw DepExtractException.InvalidInput($"{paramName} must not contain '..': {relative}");
        }

        var combined = Path.GetFullPath(Path.Combine(rootDir, relative));
        var rootFull = Path.GetFullPath(rootDir)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!combined.Equals(Path.GetFullPath(rootDir), StringComparison.OrdinalIgnoreCase)
            && !combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
        {
            throw DepExtractException.InvalidInput($"{paramName} escapes the source root: {relative}");
        }

        if (requireFile && !File.Exists(combined))
            throw DepExtractException.NotFound($"{paramName} not found under source root: {relative}");
        if (requireDir && !Directory.Exists(combined))
            throw DepExtractException.NotFound($"{paramName} directory not found under source root: {relative}");

        // Return the normalized relative form (forward slashes) for docker/cli.
        var rel = Path.GetRelativePath(rootDir, combined).Replace('\\', '/');
        return rel is "." ? "." : rel;
    }

    public static string[] ResolveEntries(string rootDir, IEnumerable<string> entries)
    {
        var list = entries?.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToArray() ?? [];
        if (list.Length == 0)
            throw DepExtractException.InvalidInput("At least one entry file is required.");
        return list.Select(e => ResolveUnderRoot(rootDir, e, "entry", requireFile: true)).ToArray();
    }
}
