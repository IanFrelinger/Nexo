namespace Nexo.Feature.AI;

/// <summary>
/// AI operation modes for demo and production use
/// </summary>
public enum AiMode
{
    /// <summary>
    /// Off mode - deterministic, offline, no network calls
    /// </summary>
    Off,
    
    /// <summary>
    /// Assist mode - scaffold generation, no runtime AI
    /// </summary>
    Assist,
    
    /// <summary>
    /// Hybrid mode - optional local AI processing
    /// </summary>
    Hybrid,
    
    /// <summary>
    /// Embedded mode - required cloud AI processing
    /// </summary>
    Embedded,
    
    /// <summary>
    /// Development mode with relaxed constraints (legacy)
    /// </summary>
    Development,
    
    /// <summary>
    /// Production mode with strict constraints (legacy)
    /// </summary>
    Production,
    
    /// <summary>
    /// Testing mode for unit tests (legacy)
    /// </summary>
    Testing,
    
    /// <summary>
    /// Debug mode with verbose logging (legacy)
    /// </summary>
    Debug
}
