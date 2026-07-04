namespace Nexo.CLI.Commands.Workflow;

/// <summary>Optimize candidate runtime state.</summary>
internal sealed class OptimizeCandidateRuntimeState
{
    /// <summary>Creates a new OptimizeCandidateRuntimeState instance.</summary>
    public OptimizeCandidateRuntimeState(
        OptimizeCandidatePlan candidate,
        string candidateRunId,
        string? profileProvider,
        IReadOnlyList<string> requiredModels,
        WorkflowCommand.ModelPullResult pullResult,
        IReadOnlyList<ScenarioPlan> plans)
    {
        Candidate = candidate;
        CandidateRunId = candidateRunId;
        ProfileProvider = profileProvider;
        RequiredModels = requiredModels;
        PullResult = pullResult;
        Plans = plans;
    }

    /// <summary>Candidate.</summary>
    public OptimizeCandidatePlan Candidate { get; }
    /// <summary>Candidate run id.</summary>
    public string CandidateRunId { get; }
    /// <summary>Profile provider.</summary>
    public string? ProfileProvider { get; }
    /// <summary>Required models.</summary>
    public IReadOnlyList<string> RequiredModels { get; }
    /// <summary>Pull result.</summary>
    public WorkflowCommand.ModelPullResult PullResult { get; }
    /// <summary>Plans.</summary>
    public IReadOnlyList<ScenarioPlan> Plans { get; }
    /// <summary>Next plan index.</summary>
    public int NextPlanIndex { get; set; }
    /// <summary>Early stopped.</summary>
    public bool EarlyStopped { get; set; }
    /// <summary>Runs.</summary>
    public List<WorkflowStressRunRecord> Runs { get; } = new();
}
