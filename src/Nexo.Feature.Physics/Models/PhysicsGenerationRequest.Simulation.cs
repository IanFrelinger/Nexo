using System.Collections.Generic;
using System.Numerics;

namespace Nexo.Feature.Physics.Models;

/// <summary>
/// Physics simulation data and objects
/// </summary>
public partial class PhysicsGenerationRequest
{
    // Simulation models are defined in separate files
}

/// <summary>
/// Physics simulation data
/// </summary>
public record PhysicsSimulationData
{
    /// <summary>
    /// Simulation name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Duration in seconds
    /// </summary>
    public float Duration { get; init; }
    
    /// <summary>
    /// Time step
    /// </summary>
    public float TimeStep { get; init; }
    
    /// <summary>
    /// Gravity vector
    /// </summary>
    public Vector3 Gravity { get; init; }
    
    /// <summary>
    /// World bounds
    /// </summary>
    public Bounds WorldBounds { get; init; } = new Bounds(new Vector3(0, 0, 0), new Vector3(100, 100, 100));
    
    /// <summary>
    /// Physics objects in the simulation
    /// </summary>
    public List<PhysicsObject> Objects { get; init; } = new();
    
    /// <summary>
    /// Simulation steps
    /// </summary>
    public List<SimulationStep> Steps { get; init; } = new();
}

/// <summary>
/// Physics object
/// </summary>
public record PhysicsObject
{
    /// <summary>
    /// Object ID
    /// </summary>
    public string Id { get; init; } = string.Empty;
    
    /// <summary>
    /// Object name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Object type
    /// </summary>
    public PhysicsObjectType Type { get; init; }
    
    /// <summary>
    /// Initial position
    /// </summary>
    public Vector3 Position { get; init; }
    
    /// <summary>
    /// Initial rotation
    /// </summary>
    public Quaternion Rotation { get; init; }
    
    /// <summary>
    /// Initial velocity
    /// </summary>
    public Vector3 Velocity { get; init; }
    
    /// <summary>
    /// Initial angular velocity
    /// </summary>
    public Vector3 AngularVelocity { get; init; }
    
    /// <summary>
    /// Mass
    /// </summary>
    public float Mass { get; init; } = 1.0f;
    
    /// <summary>
    /// Collision shape
    /// </summary>
    public CollisionShape Shape { get; init; } = new();
    
    /// <summary>
    /// Material properties
    /// </summary>
    public PhysicsMaterial Material { get; init; } = new();
}
