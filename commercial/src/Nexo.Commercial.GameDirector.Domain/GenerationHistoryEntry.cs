using System.Text.Json;
using Nexo.Commercial.GameDomain.Session;

namespace GameDirector.Domain;
public sealed record GenerationHistoryEntry(
    string AuditId,
    string TargetType,
    string Prompt,
    DateTimeOffset CreatedAtUtc);
