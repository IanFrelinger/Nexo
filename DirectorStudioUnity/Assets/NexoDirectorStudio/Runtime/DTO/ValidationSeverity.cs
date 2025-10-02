namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Severity levels for validation issues.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Information only, not an issue.
        /// </summary>
        Info = 0,
        
        /// <summary>
        /// Minor issue that doesn't prevent functionality.
        /// </summary>
        Warning = 1,
        
        /// <summary>
        /// Major issue that may affect functionality.
        /// </summary>
        Error = 2,
        
        /// <summary>
        /// Critical issue that prevents functionality.
        /// </summary>
        Critical = 3
    }
}
