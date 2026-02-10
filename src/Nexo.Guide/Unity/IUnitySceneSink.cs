using System.Text.Json;

namespace Nexo.Guide.Unity;

/// <summary>
/// Sink for 3D scene commands that Unity (or another consumer) can read.
/// Guide's 3D rendering pushes commands here so the rest of the Nexo stack (e.g. Unity viewer) can consume them.
/// </summary>
public interface IUnitySceneSink
{
    void Push(string commandType, JsonElement payload);
    void Clear();
    IReadOnlyList<SceneCommand> Snapshot();
}

public record SceneCommand(string Type, JsonElement Payload);
