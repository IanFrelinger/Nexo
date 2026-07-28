namespace Nexo.Agents.TestKit;

/// <summary>
/// Scoped environment for offline CI runs: turns the compile gate off, tells the
/// profile stack to skip the model path, and points WORKSPACE_ROOT at a temp
/// directory that is deleted on dispose. Restores every variable it touched —
/// including unsetting ones that were previously absent — so tests do not leak
/// state into each other.
/// </summary>
public sealed class OfflineAgentEnvironment : IDisposable
{
    /// <summary>Disables the toolchain compile gate.</summary>
    public const string CompileGate = "COMPILE_GATE";

    /// <summary>Tells profiles to skip the model path.</summary>
    public const string SkipModel = "SKIP_OLLAMA";

    /// <summary>Workspace containment root.</summary>
    public const string WorkspaceRoot = "WORKSPACE_ROOT";

    private readonly Dictionary<string, string?> _prior = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool _ownsRoot;
    private bool _disposed;

    /// <summary>Creates the scope and applies the offline knobs.</summary>
    /// <param name="workspaceRoot">
    /// Existing directory to use as the workspace root; when null a temp directory is
    /// created and removed on dispose.
    /// </param>
    /// <param name="extra">Additional variables to set for the duration of the scope.</param>
    public OfflineAgentEnvironment(
        string? workspaceRoot = null,
        IReadOnlyDictionary<string, string?>? extra = null)
    {
        if (workspaceRoot is null)
        {
            Root = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), "nexo-agent-" + Guid.NewGuid().ToString("n")[..12])).FullName;
            _ownsRoot = true;
        }
        else
        {
            Root = Path.GetFullPath(workspaceRoot);
            Directory.CreateDirectory(Root);
        }

        Set(CompileGate, "0");
        Set(SkipModel, "1");
        Set(WorkspaceRoot, Root);

        if (extra is not null)
        {
            foreach (var (key, value) in extra)
                Set(key, value);
        }
    }

    /// <summary>The workspace root in force for this scope.</summary>
    public string Root { get; }

    /// <summary>True when the offline knobs are currently in force.</summary>
    public static bool IsOffline =>
        string.Equals(Environment.GetEnvironmentVariable(SkipModel), "1", StringComparison.Ordinal);

    /// <summary>Creates a file under the workspace root and returns its full path.</summary>
    public string WriteFile(string relativePath, string content)
    {
        var full = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    /// <summary>Creates a directory under the workspace root and returns its full path.</summary>
    public string CreateDirectory(string relativePath)
    {
        var full = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(full);
        return full;
    }

    private void Set(string key, string? value)
    {
        if (!_prior.ContainsKey(key))
            _prior[key] = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var (key, value) in _prior)
            Environment.SetEnvironmentVariable(key, value);

        if (_ownsRoot)
        {
            try { Directory.Delete(Root, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }
}
