using System.Text.Json.Serialization;
using Nexo.Spike.S0.Models;

namespace Nexo.Spike.S0.Models;

/// <summary>
/// Per-run instrumentation — the spike deliverable is the reasoning, not just green.
/// </summary>
public sealed class SpikeRunLog
{
    public required string RunId { get; init; }
    public required string IntentId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; set; }
    public SpikeRunOutcome Outcome { get; set; }
    public GuardKind RejectingGuard { get; set; }
    public SpikeStage? RejectedAtStage { get; set; }
    public int RedGreenBounces { get; set; }
    public string? HumanVerdictNote { get; set; }
    public List<SpikeStageRecord> Stages { get; } = [];
    public List<MutationVerifyRecord> VerifyPasses { get; } = [];
    public List<PropertyVerifyRecord> PropertyPasses { get; } = [];

    public void RecordProperty(bool passed, int failedTests, string summary)
    {
        PropertyPasses.Add(new PropertyVerifyRecord
        {
            At = DateTimeOffset.UtcNow,
            Passed = passed,
            FailedTests = failedTests,
            Summary = summary
        });
    }

    public void RecordStage(SpikeStage stage, string summary, object? details = null)
    {
        Stages.Add(new SpikeStageRecord
        {
            Stage = stage,
            At = DateTimeOffset.UtcNow,
            Summary = summary,
            Details = details
        });
    }

    public void RecordVerify(double mutationScore, IReadOnlyList<string> survivingMutants, bool passed)
    {
        VerifyPasses.Add(new MutationVerifyRecord
        {
            At = DateTimeOffset.UtcNow,
            MutationScore = mutationScore,
            SurvivingMutants = survivingMutants.ToList(),
            Passed = passed
        });
    }
}

public sealed record SpikeStageRecord
{
    public required SpikeStage Stage { get; init; }
    public required DateTimeOffset At { get; init; }
    public required string Summary { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Details { get; init; }
}

public sealed record PropertyVerifyRecord
{
    public required DateTimeOffset At { get; init; }
    public required bool Passed { get; init; }
    public required int FailedTests { get; init; }
    public required string Summary { get; init; }
}

public sealed record MutationVerifyRecord
{
    public required DateTimeOffset At { get; init; }
    public required double MutationScore { get; init; }
    public required IReadOnlyList<string> SurvivingMutants { get; init; }
    public required bool Passed { get; init; }
}
