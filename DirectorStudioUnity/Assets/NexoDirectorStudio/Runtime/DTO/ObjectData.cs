using System;
using System.Collections.Generic;
using UnityEngine;

namespace NexoDirectorStudio.DTO
{
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
}
