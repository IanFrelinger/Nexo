using System.Text.Json;
using Nexo.Guide.Unity;

namespace Nexo.Guide.Rendering.Backends;

/// <summary>
/// 3D surface that stores elements in-memory and pushes commands to <see cref="IUnitySceneSink"/>
/// so the rest of the Nexo stack (e.g. Unity viewer in src) can consume them.
/// </summary>
public class ThreeJsRenderSurface : IRenderSurface3D
{
    private readonly Dictionary<string, object> _elements = new();
    private readonly IUnitySceneSink? _unitySink;

    public ThreeJsRenderSurface(IUnitySceneSink? unitySink = null)
    {
        _unitySink = unitySink;
    }

    public string? CreateMesh(CreateMeshInput input)
    {
        var id = input.Id ?? Guid.NewGuid().ToString("N")[..8];
        _elements[id] = input;
        Push("CreateMesh", input);
        return id;
    }

    public void SetMaterial(SetMaterialInput input)
    {
        Push("SetMaterial", input);
    }

    public void Transform(TransformInput input)
    {
        Push("Transform", input);
    }

    public void SetCamera(SetCameraInput input)
    {
        Push("SetCamera", input);
    }

    public string? CreateLight(CreateLightInput input)
    {
        var id = input.Id ?? Guid.NewGuid().ToString("N")[..8];
        _elements[id] = input;
        Push("CreateLight", input);
        return id;
    }

    public void Animate(Animate3DInput input)
    {
        Push("Animate3D", input);
    }

    public string? CreateParticles(CreateParticlesInput input)
    {
        var id = input.Id ?? Guid.NewGuid().ToString("N")[..8];
        _elements[id] = input;
        Push("CreateParticles", input);
        return id;
    }

    public string? Create3DText(Create3DTextInput input)
    {
        var id = input.Id ?? Guid.NewGuid().ToString("N")[..8];
        _elements[id] = input;
        Push("Create3DText", input);
        return id;
    }

    public string? Group(GroupInput input)
    {
        var id = input.Id ?? Guid.NewGuid().ToString("N")[..8];
        _elements[id] = input;
        Push("Group", input);
        return id;
    }

    public void AddClickHandler(ClickHandlerInput input)
    {
        Push("ClickHandler", input);
    }

    public void SetHoverEffect(HoverEffectInput input)
    {
        Push("HoverEffect", input);
    }

    public void Clear()
    {
        _elements.Clear();
        _unitySink?.Clear();
    }

    public void Remove(string id)
    {
        _elements.Remove(id);
    }

    private void Push<T>(string commandType, T payload)
    {
        if (_unitySink == null) return;
        try
        {
            var el = JsonSerializer.SerializeToElement(payload);
            _unitySink.Push(commandType, el);
        }
        catch
        {
            // ignore serialization errors
        }
    }
}
