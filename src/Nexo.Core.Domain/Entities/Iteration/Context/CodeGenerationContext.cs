using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities.Iteration.Requirements;

namespace Nexo.Core.Domain.Entities.Iteration.Context
{
    /// <summary>
    /// Context for code generation operations
    /// </summary>
    public record CodeGenerationContext
    {
        /// <summary>
        /// Target platform for code generation
        /// </summary>
        public string TargetPlatform { get; init; } = string.Empty;
        
        /// <summary>
        /// Programming language to generate
        /// </summary>
        public string Language { get; init; } = "C#";
        
        /// <summary>
        /// Framework or runtime to target
        /// </summary>
        public string Framework { get; init; } = ".NET";
        
        /// <summary>
        /// Version of the framework
        /// </summary>
        public string Version { get; init; } = "8.0";
        
        /// <summary>
        /// Iteration requirements
        /// </summary>
        public IterationRequirements Requirements { get; init; } = new();
        
        /// <summary>
        /// Additional configuration options
        /// </summary>
        public Dictionary<string, object> Options { get; init; } = new();
        
        /// <summary>
        /// Whether to include comments
        /// </summary>
        public bool IncludeComments { get; init; } = true;
        
        /// <summary>
        /// Whether to include error handling
        /// </summary>
        public bool IncludeErrorHandling { get; init; } = true;
        
        /// <summary>
        /// Whether to include logging
        /// </summary>
        public bool IncludeLogging { get; init; } = false;
    }
}
