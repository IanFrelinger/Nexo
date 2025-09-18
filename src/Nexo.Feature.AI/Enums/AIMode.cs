namespace Nexo.Feature.AI.Enums;

/// <summary>
/// AI operation modes
/// </summary>
public enum AiMode
{
    /// <summary>
    /// Development mode with relaxed constraints
    /// </summary>
    Development,
    
    /// <summary>
    /// Production mode with strict constraints
    /// </summary>
    Production,
    
    /// <summary>
    /// Testing mode for unit tests
    /// </summary>
    Testing,
    
    /// <summary>
    /// Debug mode with verbose logging
    /// </summary>
    Debug
}
