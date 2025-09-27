using System.Numerics;

namespace Nexo.Feature.Physics.Models;

/// <summary>
/// Physics types and enums
/// </summary>
public partial class PhysicsGenerationRequest
{
    // Physics types are defined in separate files
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
