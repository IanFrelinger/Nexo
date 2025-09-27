using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Models.Export
{
    /// <summary>
    /// Result of an export operation
    /// </summary>
    public record ExportResult
    {
        /// <summary>
        /// Whether the export was successful
        /// </summary>
        public bool Success { get; init; }
        
        /// <summary>
        /// Path where the export was saved
        /// </summary>
        public string Path { get; init; } = string.Empty;
        
        /// <summary>
        /// Export path (alias for Path)
        /// </summary>
        public string ExportPath { get; init; } = string.Empty;
        
        /// <summary>
        /// Type of export performed
        /// </summary>
        public string Type { get; init; } = string.Empty;
        
        /// <summary>
        /// Artifact type
        /// </summary>
        public string ArtifactType { get; init; } = string.Empty;
        
        /// <summary>
        /// Error message if export failed
        /// </summary>
        public string? Error { get; init; }
        
        /// <summary>
        /// Message about the export
        /// </summary>
        public string? Message { get; init; }
        
        /// <summary>
        /// Files that were exported
        /// </summary>
        public List<string> Files { get; init; } = new();
        
        /// <summary>
        /// CLI path
        /// </summary>
        public string? CliPath { get; init; }
        
        /// <summary>
        /// Docker path
        /// </summary>
        public string? DockerPath { get; init; }
        
        /// <summary>
        /// Additional metadata about the export
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }
}
