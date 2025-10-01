using System;
using System.Collections.Generic;

namespace NexoDirectorStudio.DTO
{
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
        /// ID of the content bundle this proposal is for.
        /// </summary>
        public string ContentBundleId { get; init; } = "";
        
        /// <summary>
        /// List of proposed fixes.
        /// </summary>
        public IReadOnlyList<AutoFix> ProposedFixes { get; init; } = Array.Empty<AutoFix>();
        
        /// <summary>
        /// Confidence level of the proposal (0-1).
        /// </summary>
        public float Confidence { get; init; }
        
        /// <summary>
        /// Reasoning behind the proposal.
        /// </summary>
        public string Reasoning { get; init; } = "";
        
        /// <summary>
        /// Timestamp when the proposal was created.
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Represents a single auto-fix suggestion.
    /// </summary>
    public sealed record AutoFix
    {
        /// <summary>
        /// Unique identifier for this fix.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Type of fix being proposed.
        /// </summary>
        public string FixType { get; init; } = "";
        
        /// <summary>
        /// Description of the fix.
        /// </summary>
        public string Description { get; init; } = "";
        
        /// <summary>
        /// Priority of the fix (1-5).
        /// </summary>
        public int Priority { get; init; } = 3;
        
        /// <summary>
        /// Whether the fix can be applied automatically.
        /// </summary>
        public bool CanAutoApply { get; init; }
        
        /// <summary>
        /// Steps to apply the fix.
        /// </summary>
        public IReadOnlyList<string> Steps { get; init; } = Array.Empty<string>();
    }
}
