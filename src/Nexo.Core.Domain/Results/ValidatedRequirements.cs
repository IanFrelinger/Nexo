using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Represents validated requirements for a feature
    /// </summary>
    public class ValidatedRequirements
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> FunctionalRequirements { get; set; } = new();
        public List<string> NonFunctionalRequirements { get; set; } = new();
        public List<string> Constraints { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public ValidationStatus Status { get; set; }
        public DateTime ValidatedAt { get; set; }
        public string ValidatedBy { get; set; } = string.Empty;
        public List<string> ValidationNotes { get; set; } = new();
    }

    public enum ValidationStatus
    {
        Pending,
        Validated,
        Rejected,
        NeedsRevision
    }
}
