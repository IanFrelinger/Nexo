namespace Nexo.Guide.Sandbox;

public class SandboxPolicy
{
    public string[] AllowedNamespaces { get; init; } = new[]
    {
        "Nexo.Guide.Bricks.Render2D",
        "Nexo.Guide.Bricks.Render3D",
        "Nexo.Guide.Bricks.Interaction",
        "Nexo.Guide.Bricks.Chat"
    };
    public int MaxBricksPerTurn { get; init; } = 50;
    public TimeSpan TurnTimeout { get; init; } = TimeSpan.FromSeconds(10);
    public int MaxSceneElements { get; init; } = 200;
    public int MaxTotalVertices { get; init; } = 50_000;
    public int MaxTotalParticles { get; init; } = 500;
    public bool AllowPersistence => false;
    public bool AllowNetworkAccess => false;
    public bool SuspendOnBackground { get; init; } = true;
}
