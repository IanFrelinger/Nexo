namespace Nexo.Spike.S0;

public sealed class SpikeLoopOptions
{
    public required string WorkspaceRoot { get; init; }
    public required string IntentPath { get; init; }
    public double MutationThresholdPercent { get; init; } = 80;
    public int MaxRedGreenBounces { get; init; } = 3;
    public bool CommitPerStage { get; init; }
    public bool SkipMutation { get; init; }
    public bool SkipProperty { get; init; }
    public string? RunId { get; init; }
}
