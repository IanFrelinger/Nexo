namespace Nexo.Infrastructure.IO;

/// <summary>
/// Infrastructure-only wrappers around raw filesystem APIs.
/// Keeps System.IO.File usage out of higher layers (CLI/Agents/Orchestration).
/// </summary>
public static class TextFile
{
    public static string ReadAllText(string path) => File.ReadAllText(path);

    public static Task<string> ReadAllTextAsync(string path, CancellationToken ct = default)
        => File.ReadAllTextAsync(path, ct);

    public static Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
        => File.WriteAllTextAsync(path, content, ct);

    public static Task<string[]> ReadAllLinesAsync(string path, CancellationToken ct = default)
        => File.ReadAllLinesAsync(path, ct);
}

