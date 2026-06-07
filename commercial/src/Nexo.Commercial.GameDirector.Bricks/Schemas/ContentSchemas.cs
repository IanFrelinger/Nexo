namespace GameDirector.Bricks.Schemas;

public sealed record ContentGenerationInput(
    string SessionId,
    string Prompt,
    string TargetType,
    bool Fast);

public sealed record ContentGenerationOutput(
    string AuditId,
    string Content,
    string ModelUsed,
    int Tokens);
