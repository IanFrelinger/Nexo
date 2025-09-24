using System.Collections.Generic;
using System.Numerics;

namespace Nexo.Feature.Physics.Models;

/// <summary>
/// Request for physics simulation generation
/// </summary>
public record PhysicsGenerationRequest
{
    /// <summary>
    /// Text prompt describing the physics simulation to generate
    /// </summary>
    public string Prompt { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of physics simulation
    /// </summary>
    public PhysicsType PhysicsType { get; init; } = PhysicsType.RigidBody;
    
    /// <summary>
    /// Duration of the simulation in seconds
    /// </summary>
    public float Duration { get; init; } = 5.0f;
    
    /// <summary>
    /// Time step for the simulation
    /// </summary>
    public float TimeStep { get; init; } = 0.016f; // 60 FPS
    
    /// <summary>
    /// Gravity vector
    /// </summary>
    public Vector3 Gravity { get; init; } = new Vector3(0, -9.81f, 0);
    
    /// <summary>
    /// World bounds
    /// </summary>
    public Bounds WorldBounds { get; init; } = new Bounds(new Vector3(0, 0, 0), new Vector3(100, 100, 100));
    
    /// <summary>
    /// Additional parameters specific to the provider
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Result of physics simulation generation
/// </summary>
public record PhysicsGenerationResult
{
    /// <summary>
    /// Whether the generation was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Generated physics simulation data
    /// </summary>
    public PhysicsSimulationData? SimulationData { get; init; }
    
    /// <summary>
    /// File path where simulation was saved
    /// </summary>
    public string? FilePath { get; init; }
    
    /// <summary>
    /// Simulation format (JSON, Binary, etc.)
    /// </summary>
    public string Format { get; init; } = "JSON";
    
    /// <summary>
    /// Duration of generated simulation
    /// </summary>
    public float Duration { get; init; }
    
    /// <summary>
    /// Number of simulation steps
    /// </summary>
    public int StepCount { get; init; }
    
    /// <summary>
    /// Error message if generation failed
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// Generation time in milliseconds
    /// </summary>
    public long GenerationTimeMs { get; init; }
}

/// <summary>
/// Types of physics simulations
/// </summary>
public enum PhysicsType
{
    /// <summary>
    /// Rigid body dynamics
    /// </summary>
    RigidBody,
    
    /// <summary>
    /// Soft body dynamics
    /// </summary>
    SoftBody,
    
    /// <summary>
    /// Fluid simulation
    /// </summary>
    Fluid,
    
    /// <summary>
    /// Cloth simulation
    /// </summary>
    Cloth,
    
    /// <summary>
    /// Particle system
    /// </summary>
    ParticleSystem,
    
    /// <summary>
    /// Explosion simulation
    /// </summary>
    Explosion,
    
    /// <summary>
    /// Custom simulation
    /// </summary>
    Custom
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

/// <summary>
/// Physics object types
/// </summary>
public enum PhysicsObjectType
{
    /// <summary>
    /// Static object (doesn't move)
    /// </summary>
    Static,
    
    /// <summary>
    /// Dynamic object (affected by forces)
    /// </summary>
    Dynamic,
    
    /// <summary>
    /// Kinematic object (moved by code)
    /// </summary>
    Kinematic,
    
    /// <summary>
    /// Trigger object (detects collisions but doesn't block)
    /// </summary>
    Trigger
}

/// <summary>
/// Collision shape
/// </summary>
public record CollisionShape
{
    /// <summary>
    /// Shape type
    /// </summary>
    public ShapeType Type { get; init; } = ShapeType.Box;
    
    /// <summary>
    /// Shape dimensions
    /// </summary>
    public Vector3 Dimensions { get; init; } = Vector3.One;
    
    /// <summary>
    /// Shape radius (for spheres and capsules)
    /// </summary>
    public float Radius { get; init; } = 0.5f;
    
    /// <summary>
    /// Shape height (for capsules)
    /// </summary>
    public float Height { get; init; } = 1.0f;
}

/// <summary>
/// Shape types
/// </summary>
public enum ShapeType
{
    /// <summary>
    /// Box shape
    /// </summary>
    Box,
    
    /// <summary>
    /// Sphere shape
    /// </summary>
    Sphere,
    
    /// <summary>
    /// Capsule shape
    /// </summary>
    Capsule,
    
    /// <summary>
    /// Cylinder shape
    /// </summary>
    Cylinder,
    
    /// <summary>
    /// Mesh shape
    /// </summary>
    Mesh
}

/// <summary>
/// Physics material
/// </summary>
public record PhysicsMaterial
{
    /// <summary>
    /// Material name
    /// </summary>
    public string Name { get; init; } = "Default";
    
    /// <summary>
    /// Friction coefficient
    /// </summary>
    public float Friction { get; init; } = 0.5f;
    
    /// <summary>
    /// Restitution (bounciness)
    /// </summary>
    public float Restitution { get; init; } = 0.0f;
    
    /// <summary>
    /// Density
    /// </summary>
    public float Density { get; init; } = 1.0f;
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

/// <summary>
/// Bounds structure
/// </summary>
public record Bounds
{
    /// <summary>
    /// Center point
    /// </summary>
    public Vector3 Center { get; init; }
    
    /// <summary>
    /// Size/extents
    /// </summary>
    public Vector3 Size { get; init; }
    
    /// <summary>
    /// Minimum corner
    /// </summary>
    public Vector3 Min => Center - Size * 0.5f;
    
    /// <summary>
    /// Maximum corner
    /// </summary>
    public Vector3 Max => Center + Size * 0.5f;
}
