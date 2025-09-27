using System.Collections.Generic;
using System.Numerics;

namespace Nexo.Feature.Physics.Models;

/// <summary>
/// Simulation steps and events
/// </summary>
public partial class PhysicsGenerationRequest
{
    // Step models are defined in separate files
}

/// <summary>
/// Simulation step
/// </summary>
public record SimulationStep
{
    /// <summary>
    /// Time of this step
    /// </summary>
    public float Time { get; init; }
    
    /// <summary>
    /// Object states at this step
    /// </summary>
    public List<ObjectState> ObjectStates { get; init; } = new();
    
    /// <summary>
    /// Collision events at this step
    /// </summary>
    public List<CollisionEvent> Collisions { get; init; } = new();
}

/// <summary>
/// Object state at a specific time
/// </summary>
public record ObjectState
{
    /// <summary>
    /// Object ID
    /// </summary>
    public string ObjectId { get; init; } = string.Empty;
    
    /// <summary>
    /// Position
    /// </summary>
    public Vector3 Position { get; init; }
    
    /// <summary>
    /// Rotation
    /// </summary>
    public Quaternion Rotation { get; init; }
    
    /// <summary>
    /// Velocity
    /// </summary>
    public Vector3 Velocity { get; init; }
    
    /// <summary>
    /// Angular velocity
    /// </summary>
    public Vector3 AngularVelocity { get; init; }
    
    /// <summary>
    /// Linear momentum
    /// </summary>
    public Vector3 LinearMomentum { get; init; }
    
    /// <summary>
    /// Angular momentum
    /// </summary>
    public Vector3 AngularMomentum { get; init; }
}

/// <summary>
/// Collision event
/// </summary>
public record CollisionEvent
{
    /// <summary>
    /// Time of collision
    /// </summary>
    public float Time { get; init; }
    
    /// <summary>
    /// First object ID
    /// </summary>
    public string Object1Id { get; init; } = string.Empty;
    
    /// <summary>
    /// Second object ID
    /// </summary>
    public string Object2Id { get; init; } = string.Empty;
    
    /// <summary>
    /// Collision point
    /// </summary>
    public Vector3 Point { get; init; }
    
    /// <summary>
    /// Collision normal
    /// </summary>
    public Vector3 Normal { get; init; }
    
    /// <summary>
    /// Collision force
    /// </summary>
    public Vector3 Force { get; init; }
    
    /// <summary>
    /// Collision impulse
    /// </summary>
    public Vector3 Impulse { get; init; }
}
