using System;
using System.Collections.Generic;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents a delta plan for applying changes to a game plan.
    /// </summary>
    public sealed record DeltaPlan
    {
        /// <summary>
        /// Unique identifier for this delta plan.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// ID of the original game plan this delta is based on.
        /// </summary>
        public string OriginalGamePlanId { get; init; } = "";
        
        /// <summary>
        /// List of changes to apply.
        /// </summary>
        public IReadOnlyList<GamePlanChange> Changes { get; init; } = Array.Empty<GamePlanChange>();
        
        /// <summary>
        /// Whether this delta plan has been applied.
        /// </summary>
        public bool IsApplied { get; init; }
        
        /// <summary>
        /// Timestamp when the delta plan was created.
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Represents a single change to a game plan.
    /// </summary>
    public sealed record GamePlanChange
    {
        /// <summary>
        /// Unique identifier for this change.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Type of change being made.
        /// </summary>
        public string ChangeType { get; init; } = "";
        
        /// <summary>
        /// Description of the change.
        /// </summary>
        public string Description { get; init; } = "";
        
        /// <summary>
        /// Priority of the change (1-5).
        /// </summary>
        public int Priority { get; init; } = 3;
        
        /// <summary>
        /// Whether the change can be applied automatically.
        /// </summary>
        public bool CanAutoApply { get; init; }
    }
}
