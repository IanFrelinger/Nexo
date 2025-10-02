using UnityEngine;

namespace NexoDirectorStudio.DTO
{
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
}
