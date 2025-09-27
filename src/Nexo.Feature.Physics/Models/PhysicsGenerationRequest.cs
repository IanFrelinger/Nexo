using System.Collections.Generic;
using System.Numerics;

namespace Nexo.Feature.Physics.Models;

/// <summary>
/// Physics generation request and result models
/// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
/// </summary>
public partial class PhysicsGenerationRequest
{
    // This class acts as an orchestrator for various physics generation functionalities,
    // with specific categories defined in partial classes.
}

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