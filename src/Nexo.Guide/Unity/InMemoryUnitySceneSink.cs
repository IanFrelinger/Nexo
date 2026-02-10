using System.Text.Json;

namespace Nexo.Guide.Unity;

/// <summary>
/// In-memory scene sink for 3D commands. Unity or a file/socket bridge can read from Snapshot().
/// Ephemeral: cleared when app disposes.
/// </summary>
public sealed class InMemoryUnitySceneSink : IUnitySceneSink
{
    private readonly List<SceneCommand> _commands = new();
    private readonly object _lock = new();

    public void Push(string commandType, JsonElement payload)
    {
        lock (_lock)
        {
            _commands.Add(new SceneCommand(commandType, payload.Clone()));
        }
    }

    public void Clear()
    {
        lock (_lock) _commands.Clear();
    }

    public IReadOnlyList<SceneCommand> Snapshot()
    {
        lock (_lock)
        {
            return _commands.ToList();
        }
    }
}
