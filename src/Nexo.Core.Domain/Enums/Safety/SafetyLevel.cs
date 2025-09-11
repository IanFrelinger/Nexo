namespace Nexo.Core.Domain.Enums.Safety
{
    /// <summary>
    /// Safety levels for operations and validations
    /// </summary>
    public enum SafetyLevel
    {
        /// <summary>
        /// No safety restrictions
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Basic safety checks
        /// </summary>
        Basic = 1,
        
        /// <summary>
        /// Standard safety level
        /// </summary>
        Standard = 2,
        
        /// <summary>
        /// High safety level with additional checks
        /// </summary>
        High = 3,
        
        /// <summary>
        /// Maximum safety level with all checks enabled
        /// </summary>
        Maximum = 4,
        
        /// <summary>
        /// Critical safety level for sensitive operations
        /// </summary>
        Critical = 5,
        
        /// <summary>
        /// Debug safety level with verbose logging
        /// </summary>
        Debug = 6,
        
        /// <summary>
        /// Production safety level
        /// </summary>
        Production = 7,
        
        /// <summary>
        /// Development safety level
        /// </summary>
        Development = 8,
        
        /// <summary>
        /// Testing safety level
        /// </summary>
        Testing = 9,
        
        /// <summary>
        /// Staging safety level
        /// </summary>
        Staging = 10
    }
}
