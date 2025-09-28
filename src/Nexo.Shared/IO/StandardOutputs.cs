using System;
using System.Collections.Generic;

namespace Nexo.Shared.IO
{
    /// <summary>
    /// Standardized output for command operations
    /// </summary>
    public class CommandOutput : BaseOutput
    {
        public string CommandName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Standardized output for agent operations
    /// </summary>
    public class AgentOutput : BaseOutput
    {
        public string AgentId { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> AgentMetadata { get; set; } = new();
    }

    /// <summary>
    /// Standardized output for validation operations
    /// </summary>
    public class ValidationOutput : BaseOutput
    {
        public string ValidationType { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> ValidationWarnings { get; set; } = new();
    }
}
