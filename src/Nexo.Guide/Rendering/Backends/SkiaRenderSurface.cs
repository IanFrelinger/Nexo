namespace Nexo.Guide.Rendering.Backends;

public class SkiaRenderSurface : IRenderSurface2D
{
    private readonly Dictionary<string, object> _elements = new();

    public string? DrawRect(DrawRectInput input)
    {
        var id = input.Id ?? Guid.NewGuid().ToString("N")[..8];
        _elements[id] = input;
        return id;
    }

    public string? DrawCircle(DrawCircleInput input)
    {
        var id = input.Id ?? Guid.NewGuid().ToString("N")[..8];
        _elements[id] = input;
        return id;
    }

    public string? DrawLine(DrawLineInput input)
    {
        var id = input.Id ?? Guid.NewGuid().ToString("N")[..8];
        _elements[id] = input;
        return id;
    }

    public string? DrawPath(DrawPathInput input)
    {
        var id = input.Id ?? Guid.NewGuid().ToString("N")[..8];
        _elements[id] = input;
        return id;
    }

    public string? DrawText(DrawTextInput input)
    {
        var id = input.Id ?? Guid.NewGuid().ToString("N")[..8];
        _elements[id] = input;
        return id;
    }

    public string? CreateChart(CreateChartInput input)
    {
        var id = input.Id ?? Guid.NewGuid().ToString("N")[..8];
        _elements[id] = input;
        return id;
    }

    public void Animate(Animate2DInput input) { }

    public void Clear() => _elements.Clear();

    public void Remove(string id) => _elements.Remove(id);
}
