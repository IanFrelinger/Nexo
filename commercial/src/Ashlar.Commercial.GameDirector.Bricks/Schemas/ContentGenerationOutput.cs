namespace GameDirector.Bricks.Schemas;

/// <summary>Output from AI content generation bricks.</summary>
/// <param name="AuditId">Audit record id for traceability.</param>
/// <param name="Content">Generated content payload.</param>
/// <param name="ModelUsed">Model id used for generation.</param>
/// <param name="Tokens">Approximate token count consumed.</param>
public sealed record ContentGenerationOutput(
    string AuditId,
    string Content,
    string ModelUsed,
    int Tokens);
