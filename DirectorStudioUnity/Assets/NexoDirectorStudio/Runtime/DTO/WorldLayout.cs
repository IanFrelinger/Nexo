using System;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents the spatial layout of the game world, including tiles, objects, and navigation.
    /// This is the output of the world building phase.
    /// </summary>
    public sealed record WorldLayout(
        string Id,
        string Name,
        string GamePlanId,
        Vector3 Dimensions,
        IReadOnlyList<TileData> Tiles,
        IReadOnlyList<ObjectData> Objects,
        IReadOnlyList<NavigationNode> NavigationNodes,
        LightingData Lighting,
        CameraData Camera,
        int Seed,
        DateTimeOffset GeneratedAt)
    {
        public string Id { get; init; } = Id;
        public string Name { get; init; } = Name;
        public string GamePlanId { get; init; } = GamePlanId;
        public Vector3 Dimensions { get; init; } = Dimensions;
        public IReadOnlyList<TileData> Tiles { get; init; } = Tiles;
        public IReadOnlyList<ObjectData> Objects { get; init; } = Objects;
        public IReadOnlyList<NavigationNode> NavigationNodes { get; init; } = NavigationNodes;
        public LightingData Lighting { get; init; } = Lighting;
        public CameraData Camera { get; init; } = Camera;
        public int Seed { get; init; } = Seed;
        public DateTimeOffset GeneratedAt { get; init; } = GeneratedAt;
    }
    
    /// <summary>
    /// Represents a single tile in the world grid.
    /// </summary>
    public sealed record TileData
    {
        /// <summary>
        /// Grid position of the tile.
        /// </summary>
        public Vector2Int GridPosition { get; init; }
        
        /// <summary>
        /// World position of the tile.
        /// </summary>
        public Vector3 WorldPosition { get; init; }
        
        /// <summary>
        /// Type of tile (e.g., "Ground", "Wall", "Platform", "Hazard").
        /// </summary>
        public string TileType { get; init; } = string.Empty;
        
        /// <summary>
        /// Material or texture identifier for the tile.
        /// </summary>
        public string MaterialId { get; init; } = string.Empty;
        
        /// <summary>
        /// Whether this tile is walkable.
        /// </summary>
        public bool IsWalkable { get; init; } = true;
        
        /// <summary>
        /// Whether this tile blocks line of sight.
        /// </summary>
        public bool BlocksLineOfSight { get; init; } = false;
        
        /// <summary>
        /// Additional properties specific to the tile type.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Represents an interactive object in the world.
    /// </summary>
    public sealed record ObjectData
    {
        /// <summary>
        /// Unique identifier for this object.
        /// </summary>
        public string Id { get; init; } = System.Guid.NewGuid().ToString();
        
        /// <summary>
        /// World position of the object.
        /// </summary>
        public Vector3 Position { get; init; }
        
        /// <summary>
        /// Rotation of the object.
        /// </summary>
        public Quaternion Rotation { get; init; } = Quaternion.identity;
        
        /// <summary>
        /// Scale of the object.
        /// </summary>
        public Vector3 Scale { get; init; } = Vector3.one;
        
        /// <summary>
        /// Type of object (e.g., "SpawnPoint", "Collectible", "Trigger", "Enemy").
        /// </summary>
        public string ObjectType { get; init; } = string.Empty;
        
        /// <summary>
        /// Prefab identifier for the object.
        /// </summary>
        public string PrefabId { get; init; } = string.Empty;
        
        /// <summary>
        /// Additional properties specific to the object type.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Represents a navigation node in the world.
    /// </summary>
    public sealed record NavigationNode
    {
        /// <summary>
        /// Unique identifier for this node.
        /// </summary>
        public string Id { get; init; } = System.Guid.NewGuid().ToString();
        
        /// <summary>
        /// World position of the node.
        /// </summary>
        public Vector3 Position { get; init; }
        
        /// <summary>
        /// Type of navigation node (e.g., "Waypoint", "Checkpoint", "Spawn", "Goal").
        /// </summary>
        public string NodeType { get; init; } = string.Empty;
        
        /// <summary>
        /// Connected node IDs.
        /// </summary>
        public IReadOnlyList<string> ConnectedNodeIds { get; init; } = Array.Empty<string>();
        
        /// <summary>
        /// Additional properties specific to the node type.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Represents lighting configuration for the world.
    /// </summary>
    public sealed record LightingData
    {
        /// <summary>
        /// Ambient light color.
        /// </summary>
        public Color AmbientColor { get; init; } = Color.white;
        
        /// <summary>
        /// Ambient light intensity.
        /// </summary>
        public float AmbientIntensity { get; init; } = 1.0f;
        
        /// <summary>
        /// Directional light direction.
        /// </summary>
        public Vector3 DirectionalLightDirection { get; init; } = Vector3.down;
        
        /// <summary>
        /// Directional light color.
        /// </summary>
        public Color DirectionalLightColor { get; init; } = Color.white;
        
        /// <summary>
        /// Directional light intensity.
        /// </summary>
        public float DirectionalLightIntensity { get; init; } = 1.0f;
    }
    
    /// <summary>
    /// Represents camera configuration for the world.
    /// </summary>
    public sealed record CameraData
    {
        /// <summary>
        /// Initial camera position.
        /// </summary>
        public Vector3 InitialPosition { get; init; }
        
        /// <summary>
        /// Initial camera rotation.
        /// </summary>
        public Quaternion InitialRotation { get; init; } = Quaternion.identity;
        
        /// <summary>
        /// Camera field of view.
        /// </summary>
        public float FieldOfView { get; init; } = 60.0f;
        
        /// <summary>
        /// Camera movement constraints.
        /// </summary>
        public CameraConstraints Constraints { get; init; } = new();
    }
    
    /// <summary>
    /// Represents camera movement constraints.
    /// </summary>
    public sealed record CameraConstraints
    {
        /// <summary>
        /// Minimum camera position.
        /// </summary>
        public Vector3 MinPosition { get; init; } = Vector3.negativeInfinity;
        
        /// <summary>
        /// Maximum camera position.
        /// </summary>
        public Vector3 MaxPosition { get; init; } = Vector3.positiveInfinity;
        
        /// <summary>
        /// Whether camera can rotate.
        /// </summary>
        public bool CanRotate { get; init; } = true;
        
        /// <summary>
        /// Whether camera can zoom.
        /// </summary>
        public bool CanZoom { get; init; } = true;
    }
}
