using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Reporting;

public sealed record SkillLoopCallRecord(
    string Scenario,
    string? ContextLabel,
    EnsureSkillOutcome Outcome,
    bool GenerationRan,
    bool CertificationRan,
    bool Reused,
    string GeneratorBackend,
    string? EntryId,
    string? RejectionReason,
    int RegistryEntryCountBefore,
    int RegistryEntryCountAfter,
    bool? IsolationEnforced = null,
    string? ModelId = null,
    int? AttemptsUsed = null,
    double? Temperature = null);

public sealed record SkillLoopReport(
    string Version,
    string GeneratorBackend,
    DateTimeOffset RunAt,
    IReadOnlyList<SkillLoopCallRecord> Calls,
    IReadOnlyList<string> RegistryEntryIds);
