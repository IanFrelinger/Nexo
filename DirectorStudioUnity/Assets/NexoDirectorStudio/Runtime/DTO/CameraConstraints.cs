using UnityEngine;

namespace NexoDirectorStudio.DTO
{
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
