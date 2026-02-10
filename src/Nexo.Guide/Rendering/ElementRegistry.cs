namespace Nexo.Guide.Rendering;

/// <summary>
/// In-memory only. Tracks all rendered elements.
/// Dies with the app. No serialization. No persistence.
/// </summary>
public class ElementRegistry
{
    private readonly Dictionary<string, string> _elements = new(); // id → brick type

    public void Track(string id, string brickType) => _elements[id] = brickType;
    public bool Exists(string id) => _elements.ContainsKey(id);
    public int Count => _elements.Count;
    public void Clear() => _elements.Clear();
}
