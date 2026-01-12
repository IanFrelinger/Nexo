namespace Nexo.Agents.AutonomousDev.Models;

/// <summary>
/// Persona for the mock user in testing.
/// </summary>
public enum MockUserPersona
{
    /// <summary>
    /// First-time user, might miss obvious things.
    /// </summary>
    Novice,
    
    /// <summary>
    /// Typical user, follows expected paths.
    /// </summary>
    Average,
    
    /// <summary>
    /// Expert user, tries shortcuts and edge cases.
    /// </summary>
    PowerUser,
    
    /// <summary>
    /// Actively tries to break things.
    /// </summary>
    Adversarial,
    
    /// <summary>
    /// User with accessibility needs.
    /// </summary>
    Accessibility,
    
    /// <summary>
    /// Impatient user, clicks rapidly, doesn't wait.
    /// </summary>
    Impatient
}
