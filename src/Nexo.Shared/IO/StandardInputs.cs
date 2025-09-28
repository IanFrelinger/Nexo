using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexo.Shared.IO
{
    /// <summary>
    /// Standardized input for command operations
    /// </summary>
    public class CommandInput : BaseInput
    {
        [Required]
        public string CommandName { get; set; } = string.Empty;
        
        public string? TargetId { get; set; }
        public Dictionary<string, object> CommandParameters { get; set; } = new();
    }

    /// <summary>
    /// Standardized input for agent operations
    /// </summary>
    public class AgentInput : BaseInput
    {
        [Required]
        public string AgentId { get; set; } = string.Empty;
        
        public string TaskType { get; set; } = string.Empty;
        public Dictionary<string, object> TaskParameters { get; set; } = new();
    }

    /// <summary>
    /// Standardized input for validation operations
    /// </summary>
    public class ValidationInput : BaseInput
    {
        [Required]
        public string ValidationType { get; set; } = string.Empty;
        
        public object? TargetObject { get; set; }
        public Dictionary<string, object> ValidationRules { get; set; } = new();
    }
}
