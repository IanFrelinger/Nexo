using UnityEngine;

namespace NexoDirectorStudio.DTO
{
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
}
