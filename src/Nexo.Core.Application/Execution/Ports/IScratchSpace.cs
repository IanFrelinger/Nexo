namespace Nexo.Core.Application.Execution.Ports;

/// <summary>
/// Creates disposable scratch directories for draft/validate work.
/// Domain-neutral — no toolchain vocabulary.
/// </summary>
public interface IScratchSpace
{
    /// <summary>Creates a uniquely named scratch directory with the given prefix.</summary>
    IScratchDir CreateScratchDir(string prefix);
}

/// <summary>Disposable scratch directory.</summary>
public interface IScratchDir : IDisposable
{
    /// <summary>Absolute path of the scratch directory.</summary>
    string Path { get; }
}

/// <summary>
/// Workspace path policy. When WORKSPACE_ROOT is unset, containment checks
/// are no-ops (explicitly logged by the infrastructure implementation).
/// </summary>
public interface IWorkspacePathPolicy
{
    /// <summary>Requires that <paramref name="path"/> exists and is a directory.</summary>
    void RequireExistingDirectory(string path);

    /// <summary>Resolves <paramref name="relativeOrAbsolute"/> under the workspace root when set.</summary>
    string ResolveUnderRoot(string relativeOrAbsolute);

    /// <summary>True when the path is under the workspace root (or when root is unset).</summary>
    bool IsUnderRoot(string path);

    /// <summary>Throws when the path escapes the workspace root (no-op when unset).</summary>
    void EnsureContained(string path);
}

/// <summary>Applies a set of files under a destination root with snapshot/rollback.</summary>
public interface IManagedFileSet
{
    /// <summary>Writes files and returns an apply result that can roll back.</summary>
    Task<ManagedFileApplyResult> ApplyAsync(
        string destRoot,
        IReadOnlyDictionary<string, string> files,
        CancellationToken cancellationToken = default);
}

/// <summary>Result of a managed file apply.</summary>
public sealed class ManagedFileApplyResult
{
    /// <summary>True when files were written.</summary>
    public bool Succeeded { get; init; }

    /// <summary>Rolls back to the pre-apply snapshot.</summary>
    public required Func<CancellationToken, Task> Rollback { get; init; }
}

/// <summary>Single-flight guard: concurrent callers with the same key share one execution.</summary>
public interface ISingleFlightGuard
{
    /// <summary>Runs <paramref name="work"/> exclusively for <paramref name="key"/>.</summary>
    Task<T> RunExclusiveAsync<T>(string key, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default);
}
