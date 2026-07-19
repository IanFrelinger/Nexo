using Nexo.Abstractions;

namespace Nexo.BRPlaytestAgent;

internal sealed class PlaytestDelta : IActionDelta
{
    private readonly List<string> _log = new();

    public int TickFrom { get; init; }
    public int TickTo { get; init; }
    public IReadOnlyList<byte>? Signature { get; set; }
    public IReadOnlyList<string> Log => _log;

    public void Add(string message) => _log.Add(message);
}
