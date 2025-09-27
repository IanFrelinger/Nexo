using System;

namespace Nexo.Core.Domain.Models
{
    /// <summary>
    /// Represents an evolved tool
    /// </summary>
    public partial class EvolvedTool
    {
        /// <summary>
        /// Name of the evolved tool
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// New version number
        /// </summary>
        public int Version { get; set; }
        
        /// <summary>
        /// Description of the evolution
        /// </summary>
        public string EvolutionDescription { get; set; } = string.Empty;
        
        /// <summary>
        /// When the evolution was completed
        /// </summary>
        public DateTime EvolvedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Whether the evolution was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Any errors that occurred during evolution
        /// </summary>
        public string[] Errors { get; set; } = Array.Empty<string>();
    }
}
