namespace Nexo.Guide.Rendering;

public interface IRenderSurface2D
{
    string? DrawRect(DrawRectInput input);
    string? DrawCircle(DrawCircleInput input);
    string? DrawLine(DrawLineInput input);
    string? DrawPath(DrawPathInput input);
    string? DrawText(DrawTextInput input);
    string? CreateChart(CreateChartInput input);
    void Animate(Animate2DInput input);
    void Clear();
    void Remove(string id);
}
