using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Represents an extracted requirement from natural language
    /// </summary>
    public class ExtractedRequirement
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RequirementType Type { get; set; }
        public Priority Priority { get; set; }
        public string Source { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public List<string> AcceptanceCriteria { get; set; } = new();
        public DateTime ExtractedAt { get; set; }
        public string ExtractedBy { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }

    public enum RequirementType
    {
        Functional,
        NonFunctional,
        Constraint,
        BusinessRule,
        UserStory,
        UseCase
    }

    public enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }
}