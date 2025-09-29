using Nexo.Abstractions;

namespace Nexo.Tools.Dev.Deltas;

public sealed record FileEdit(string Path, string? BeforeSha, string AfterSha, int Added, int Removed);

public sealed class RepoDelta : IActionDelta
{
    public int TickFrom { get; init; }
    public int TickTo   { get; init; }
    public byte[]? Signature { get; set; }
    public IReadOnlyList<string> Log => _log;
    public IReadOnlyList<FileEdit> Edits => _edits;

    private readonly List<string> _log = new();
    private readonly List<FileEdit> _edits = new();

    public void AddLog(string msg) => _log.Add(msg);
    public void AddEdit(FileEdit e) => _edits.Add(e);
}
