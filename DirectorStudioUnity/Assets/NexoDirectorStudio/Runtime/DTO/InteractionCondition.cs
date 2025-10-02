using System.Collections.Generic;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents a condition for an interaction.
    /// </summary>
    public sealed record InteractionCondition
    {
        /// <summary>
        /// Type of condition (e.g., "VariableEquals", "ItemInInventory", "TimeElapsed").
        /// </summary>
        public string ConditionType { get; init; } = string.Empty;
        
        /// <summary>
        /// Parameters for the condition.
        /// </summary>
        public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Whether this condition must be true or false.
        /// </summary>
        public bool MustBeTrue { get; init; } = true;
    }
}
