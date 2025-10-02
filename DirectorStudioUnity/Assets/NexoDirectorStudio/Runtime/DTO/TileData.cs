using System.Collections.Generic;
using UnityEngine;

namespace NexoDirectorStudio.DTO
{
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
}
