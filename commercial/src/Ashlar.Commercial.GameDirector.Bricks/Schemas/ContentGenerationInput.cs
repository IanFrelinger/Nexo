namespace GameDirector.Bricks.Schemas;

/// <summary>Input for AI content generation bricks.</summary>
/// <param name="SessionId">Forge session identifier.</param>
/// <param name="Prompt">Generation prompt text.</param>
/// <param name="TargetType">Target content type (e.g. map tile, dialogue).</param>
/// <param name="Fast">When true, prefer faster/cheaper model routing.</param>
public sealed record ContentGenerationInput(
    string SessionId,
    string Prompt,
    string TargetType,
    bool Fast);
