using Nexo.Guide.Rendering;
using Nexo.Guide.Sandbox;
using Nexo.Guide.Services;
using Nexo.Guide.ViewModels;

namespace Nexo.Guide.Agent.Behaviors;

public class RenderBehavior
{
    private readonly SandboxedExecutionEngine _engine;
    private readonly IRenderSurface2D _surface2D;
    private readonly IRenderSurface3D _surface3D;
    private readonly ElementRegistry _elements;

    public event Action<string>? OnNarration;
    public event Action<BrickLogEntry>? OnBrickExecuted;

    public RenderBehavior(
        SandboxedExecutionEngine engine,
        IRenderSurface2D surface2D,
        IRenderSurface3D surface3D,
        ElementRegistry elements)
    {
        _engine = engine;
        _surface2D = surface2D;
        _surface3D = surface3D;
        _elements = elements;
    }

    public async Task<RenderResult> ExecuteAsync(RenderPlan plan, CancellationToken ct)
    {
        _engine.ResetTurnBudget();

        if (plan.Clear)
        {
            if (plan.Surface is "2d" or "both") _surface2D.Clear();
            if (plan.Surface is "3d" or "both") _surface3D.Clear();
            _elements.Clear();
        }

        int totalBricks = 0;

        foreach (var step in plan.Steps)
        {
            if (!string.IsNullOrEmpty(step.Narration))
                OnNarration?.Invoke(step.Narration);

            if (step.DelayMs > 0)
                await Task.Delay(Math.Min((int)step.DelayMs, 2000), ct).ConfigureAwait(false);

            foreach (var cmd in step.Commands)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var elementId = ExecuteRenderCommand(cmd);
                sw.Stop();

                if (elementId != null)
                    _elements.Track(elementId, cmd.Brick);

                totalBricks++;

                OnBrickExecuted?.Invoke(new BrickLogEntry
                {
                    BrickName = cmd.Brick,
                    Mode = "⚙️",
                    ElapsedMs = sw.ElapsedMilliseconds
                });
            }
        }

        return new RenderResult(totalBricks, plan.Surface);
    }

    private string? ExecuteRenderCommand(RenderCommand cmd)
    {
        return cmd.Brick switch
        {
            "CreateMesh" => _surface3D.CreateMesh(cmd.Deserialize<CreateMeshInput>()),
            "SetMaterial" => Do(() => _surface3D.SetMaterial(cmd.Deserialize<SetMaterialInput>())),
            "Transform" => Do(() => _surface3D.Transform(cmd.Deserialize<TransformInput>())),
            "SetCamera" => Do(() => _surface3D.SetCamera(cmd.Deserialize<SetCameraInput>())),
            "CreateLight" => _surface3D.CreateLight(cmd.Deserialize<CreateLightInput>()),
            "Animate3D" => Do(() => _surface3D.Animate(cmd.Deserialize<Animate3DInput>())),
            "CreateParticles" => _surface3D.CreateParticles(cmd.Deserialize<CreateParticlesInput>()),
            "Create3DText" => _surface3D.Create3DText(cmd.Deserialize<Create3DTextInput>()),
            "Group" => _surface3D.Group(cmd.Deserialize<GroupInput>()),
            "DrawRect" => _surface2D.DrawRect(cmd.Deserialize<DrawRectInput>()),
            "DrawCircle" => _surface2D.DrawCircle(cmd.Deserialize<DrawCircleInput>()),
            "DrawLine" => _surface2D.DrawLine(cmd.Deserialize<DrawLineInput>()),
            "DrawPath" => _surface2D.DrawPath(cmd.Deserialize<DrawPathInput>()),
            "DrawText" => _surface2D.DrawText(cmd.Deserialize<DrawTextInput>()),
            "CreateChart" => _surface2D.CreateChart(cmd.Deserialize<CreateChartInput>()),
            "Animate2D" => Do(() => _surface2D.Animate(cmd.Deserialize<Animate2DInput>())),
            "ClickHandler" => Do(() => _surface3D.AddClickHandler(cmd.Deserialize<ClickHandlerInput>())),
            "HoverEffect" => Do(() => _surface3D.SetHoverEffect(cmd.Deserialize<HoverEffectInput>())),
            "ClearScene" => Do(() =>
            {
                var input = cmd.Deserialize<ClearSceneInput>();
                if (input.Target == null) { _surface2D.Clear(); _surface3D.Clear(); }
                else if (input.Target == "2d") _surface2D.Clear();
                else if (input.Target == "3d") _surface3D.Clear();
                else { _surface2D.Remove(input.Target); _surface3D.Remove(input.Target); }
            }),
            _ => throw new InvalidOperationException($"Unknown render brick: {cmd.Brick}")
        };
    }

    private static string? Do(Action action)
    {
        action();
        return null;
    }
}
