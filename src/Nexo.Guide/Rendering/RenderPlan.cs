using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexo.Guide.Rendering;

public class RenderPlan
{
    public string Surface { get; init; } = "2d";  // "2d", "3d", "both"
    public bool Clear { get; init; }
    public List<RenderStep> Steps { get; init; } = new();

    public static RenderPlan FromLlmOutput(JsonElement renderJson)
    {
        return JsonSerializer.Deserialize<RenderPlan>(renderJson.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Failed to parse render plan");
    }
}

public class RenderStep
{
    public string? Narration { get; init; }
    [JsonPropertyName("delay")]
    public float DelayMs { get; init; }
    public List<RenderCommand> Commands { get; init; } = new();
}

public class RenderCommand
{
    public string Brick { get; init; } = "";
    public JsonElement Input { get; init; }

    public T Deserialize<T>() where T : class =>
        JsonSerializer.Deserialize<T>(Input.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");
}

public record RenderResult(int BricksExecuted, string Surface);

// ─── Brick input types ───
// 2D
public record DrawRectInput(float X, float Y, float Width, float Height, string Fill, float CornerRadius = 0, float Opacity = 1f, string? StrokeColor = null, float StrokeWidth = 0, string? Id = null);
public record DrawCircleInput(float CenterX, float CenterY, float Radius, string Fill, float Opacity = 1f, string? Id = null);
public record DrawLineInput(float X1, float Y1, float X2, float Y2, string Color, float Width = 1f, bool Dashed = false, string? Id = null);
public record DrawPathInput(string SvgPath, string Fill, string? StrokeColor = null, float StrokeWidth = 0, string? Id = null);
public record DrawTextInput(float X, float Y, string Text, float FontSize = 14f, string FontWeight = "normal", string Color = "#F5F5F7", string Anchor = "start", string FontFamily = "sans", string? Id = null);
public record ChartDataPoint(string Label, float Value, string? Color = null);
public record CreateChartInput(string Type, float X, float Y, float Width, float Height, ChartDataPoint[] Data, string? Title = null, string[]? Colors = null, bool Animated = true, string? Id = null);
public record Animate2DInput(string TargetId, string Property, float From, float To, float DurationMs = 500f, string Easing = "ease-out", float DelayMs = 0);

// 3D
public record CreateMeshInput(string Primitive, float[] Position, float[]? Scale = null, float[]? Rotation = null, string? Id = null);
public record SetMaterialInput(string TargetId, string Color, float Metallic = 0, float Roughness = 0.5f, float Opacity = 1f, float EmissiveIntensity = 0, string? EmissiveColor = null, bool Wireframe = false);
public record TransformInput(string TargetId, float[]? Position = null, float[]? Rotation = null, float[]? Scale = null);
public record SetCameraInput(float[] Position, float[] LookAt, float Fov = 60f, bool OrbitEnabled = true);
public record CreateLightInput(string Type, string Color, float Intensity = 1f, float[]? Position = null, float[]? Direction = null, string? Id = null);
public record Animate3DInput(string TargetId, string Property, float[]? FromVec = null, float[]? ToVec = null, float? FromFloat = null, float? ToFloat = null, float DurationMs = 500f, string Easing = "ease-out", bool Loop = false);
public record CreateParticlesInput(float[] Position, int Count = 50, string Color = "#6EE7B7", float Speed = 1f, float Lifetime = 2f, string Shape = "sphere", float Size = 0.05f, string? Id = null);
public record Create3DTextInput(string Text, float[] Position, float FontSize = 0.5f, string Color = "#F5F5F7", bool Billboard = true, string? Id = null);
public record GroupInput(string[] ChildIds, float[]? Position = null, string? Id = null);
public record ClearSceneInput(string? Target = null);

// Interaction
public record HoverEffectInput(string TargetId, string Effect, string? HighlightColor = null);
public record ClickHandlerInput(string TargetId, string CallbackTag);
