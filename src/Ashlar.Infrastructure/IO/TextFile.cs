namespace Ashlar.Infrastructure.IO;

/// <summary>
/// Infrastructure-only wrappers around raw filesystem APIs.
/// Keeps System.IO.File usage out of higher layers (CLI/Agents/Orchestration).
/// </summary>
public static class TextFile
{
    /// <summary>Read all text.</summary>
    public static string ReadAllText(string path) => File.ReadAllText(path);

    /// <summary>Read all text asynchronously.</summary>
    public static Task<string> ReadAllTextAsync(string path, CancellationToken ct = default)
        => File.ReadAllTextAsync(path, ct);

    /// <summary>Write all text asynchronously.</summary>
    public static Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
        => File.WriteAllTextAsync(path, content, ct);

    /// <summary>Read all lines asynchronously.</summary>
    public static Task<string[]> ReadAllLinesAsync(string path, CancellationToken ct = default)
        => File.ReadAllLinesAsync(path, ct);
}

