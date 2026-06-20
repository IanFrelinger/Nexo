namespace Nexo.Spike.S3.Models;

public sealed record SkillProvenance(
    string GeneratorBackend,
    bool IsolationEnforced,
    string? ModelId,
    double? Temperature,
    int AttemptsUsed);
