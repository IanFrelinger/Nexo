using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Command for proposing auto-fixes for validation issues.
    /// Analyzes validation results and suggests specific fixes that can be automatically applied.
    /// </summary>
    public interface IProposeAutoFixesCommand
    {
        /// <summary>
        /// Proposes auto-fixes for a validation report.
        /// </summary>
        /// <param name="validationReport">The validation report to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of proposed auto-fixes</returns>
        Task<AutoFixProposal> ExecuteAsync(ValidationReport validationReport, CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Command for applying auto-fixes to a game plan.
    /// Takes a delta plan and applies the changes to create an improved game plan.
    /// </summary>
    public interface IApplyAutoFixesCommand
    {
        /// <summary>
        /// Applies auto-fixes to a game plan.
        /// </summary>
        /// <param name="gamePlan">The original game plan</param>
        /// <param name="deltaPlan">The delta plan containing the fixes to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated game plan</returns>
        Task<GamePlan> ExecuteAsync(GamePlan gamePlan, DeltaPlan deltaPlan, CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Represents a proposal for auto-fixing validation issues.
    /// </summary>
    public sealed record AutoFixProposal
    {
        /// <summary>
        /// Unique identifier for this proposal.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The original validation report.
        /// </summary>
        public ValidationReport OriginalReport { get; init; }
        
        /// <summary>
        /// List of proposed fixes.
        /// </summary>
        public IReadOnlyList<AutoFix> ProposedFixes { get; init; } = Array.Empty<AutoFix>();
        
        /// <summary>
        /// Estimated effort to apply all fixes.
        /// </summary>
        public string EstimatedEffort { get; init; } = "Unknown";
        
        /// <summary>
        /// Confidence score for the proposed fixes (0-100).
        /// </summary>
        public int ConfidenceScore { get; init; }
        
        /// <summary>
        /// Whether all proposed fixes can be applied automatically.
        /// </summary>
        public bool CanApplyAllFixes { get; init; }
        
        /// <summary>
        /// Summary of the proposed changes.
        /// </summary>
        public string Summary { get; init; } = "";
        
        /// <summary>
        /// Detailed description of the proposed changes.
        /// </summary>
        public string Description { get; init; } = "";
    }
    
    /// <summary>
    /// Represents a single auto-fix that can be applied to a game plan.
    /// </summary>
    public sealed record AutoFix
    {
        /// <summary>
        /// Unique identifier for this fix.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The validation issue this fix addresses.
        /// </summary>
        public ValidationIssue TargetIssue { get; init; }
        
        /// <summary>
        /// The type of fix to apply.
        /// </summary>
        public AutoFixType FixType { get; init; }
        
        /// <summary>
        /// The specific changes to make.
        /// </summary>
        public IReadOnlyList<GamePlanChange> Changes { get; init; } = Array.Empty<GamePlanChange>();
        
        /// <summary>
        /// Priority of this fix (1-5).
        /// </summary>
        public int Priority { get; init; } = 3;
        
        /// <summary>
        /// Effort required to apply this fix.
        /// </summary>
        public string Effort { get; init; } = "Medium";
        
        /// <summary>
        /// Confidence score for this fix (0-100).
        /// </summary>
        public int ConfidenceScore { get; init; }
        
        /// <summary>
        /// Whether this fix can be applied automatically.
        /// </summary>
        public bool CanApplyAutomatically { get; init; }
        
        /// <summary>
        /// Title of this fix.
        /// </summary>
        public string Title { get; init; } = "";
        
        /// <summary>
        /// Description of this fix.
        /// </summary>
        public string Description { get; init; } = "";
    }
    
    /// <summary>
    /// Types of auto-fixes that can be applied.
    /// </summary>
    public enum AutoFixType
    {
        /// <summary>
        /// Add missing mechanics to the game plan.
        /// </summary>
        AddMechanics,
        
        /// <summary>
        /// Add missing assets to the game plan.
        /// </summary>
        AddAssets,
        
        /// <summary>
        /// Adjust difficulty progression.
        /// </summary>
        AdjustDifficulty,
        
        /// <summary>
        /// Modify player experience elements.
        /// </summary>
        ModifyPlayerExperience,
        
        /// <summary>
        /// Adjust estimated duration.
        /// </summary>
        AdjustDuration,
        
        /// <summary>
        /// Add narrative beats.
        /// </summary>
        AddNarrativeBeats,
        
        /// <summary>
        /// Remove conflicting elements.
        /// </summary>
        RemoveConflicts,
        
        /// <summary>
        /// Reorder elements for better flow.
        /// </summary>
        ReorderElements
    }
    
    /// <summary>
    /// Represents a specific change to a game plan.
    /// </summary>
    public sealed record GamePlanChange
    {
        /// <summary>
        /// The type of change to make.
        /// </summary>
        public GamePlanChangeType ChangeType { get; init; }
        
        /// <summary>
        /// The path to the property to change.
        /// </summary>
        public string PropertyPath { get; init; } = "";
        
        /// <summary>
        /// The old value (for undo purposes).
        /// </summary>
        public object? OldValue { get; init; }
        
        /// <summary>
        /// The new value to set.
        /// </summary>
        public object? NewValue { get; init; }
        
        /// <summary>
        /// Additional context for the change.
        /// </summary>
        public string Context { get; init; } = "";
    }
    
    /// <summary>
    /// Types of changes that can be made to a game plan.
    /// </summary>
    public enum GamePlanChangeType
    {
        /// <summary>
        /// Add a new element to a collection.
        /// </summary>
        Add,
        
        /// <summary>
        /// Remove an element from a collection.
        /// </summary>
        Remove,
        
        /// <summary>
        /// Update an existing element.
        /// </summary>
        Update,
        
        /// <summary>
        /// Replace an entire property value.
        /// </summary>
        Replace
    }
    
    /// <summary>
    /// Represents a delta plan containing changes to apply to a game plan.
    /// </summary>
    public sealed record DeltaPlan
    {
        /// <summary>
        /// Unique identifier for this delta plan.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The original game plan ID.
        /// </summary>
        public string OriginalGamePlanId { get; init; } = "";
        
        /// <summary>
        /// List of changes to apply.
        /// </summary>
        public IReadOnlyList<GamePlanChange> Changes { get; init; } = Array.Empty<GamePlanChange>();
        
        /// <summary>
        /// Summary of the changes.
        /// </summary>
        public string Summary { get; init; } = "";
        
        /// <summary>
        /// Description of the changes.
        /// </summary>
        public string Description { get; init; } = "";
        
        /// <summary>
        /// Whether this delta plan has been applied.
        /// </summary>
        public bool IsApplied { get; init; }
        
        /// <summary>
        /// Timestamp when this delta plan was created.
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }
}
