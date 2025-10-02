namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents a variable that can be used in interactions.
    /// </summary>
    public sealed record InteractionVariable
    {
        /// <summary>
        /// Name of the variable.
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Type of the variable (e.g., "Int", "Float", "String", "Bool").
        /// </summary>
        public string VariableType { get; init; } = string.Empty;
        
        /// <summary>
        /// Initial value of the variable.
        /// </summary>
        public object? InitialValue { get; init; }
        
        /// <summary>
        /// Whether this variable is global (accessible from all interactions).
        /// </summary>
        public bool IsGlobal { get; init; } = true;
        
        /// <summary>
        /// Description of what this variable represents.
        /// </summary>
        public string Description { get; init; } = string.Empty;
    }
}
