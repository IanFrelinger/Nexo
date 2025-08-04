using System.Collections.Generic;
using Nexo.Feature.Web.Enums;

namespace Nexo.Feature.Web.Models
{
    /// <summary>
    /// Request model for web code generation.
    /// </summary>
    public class WebCodeGenerationRequest
    {
        /// <summary>
        /// The target web framework for code generation.
        /// </summary>
        public WebFrameworkType Framework { get; set; } = WebFrameworkType.React;
        
        /// <summary>
        /// The type of component to generate.
        /// </summary>
        public WebComponentType ComponentType { get; set; } = WebComponentType.Functional;
        
        /// <summary>
        /// The name of the component to generate.
        /// </summary>
        public string ComponentName { get; set; } = string.Empty;
        
        /// <summary>
        /// The source code or specification to base the generation on.
        /// </summary>
        public string SourceCode { get; set; } = string.Empty;
        
        /// <summary>
        /// The target file path for the generated code.
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;
        
        /// <summary>
        /// WebAssembly optimization strategy to apply.
        /// </summary>
        public WebAssemblyOptimization Optimization { get; set; } = WebAssemblyOptimization.Balanced;
        
        /// <summary>
        /// Additional options for code generation.
        /// </summary>
        public Dictionary<string, object> Options { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Whether to include TypeScript types.
        /// </summary>
        public bool IncludeTypeScript { get; set; } = true;
        
        /// <summary>
        /// Whether to include CSS/styling.
        /// </summary>
        public bool IncludeStyling { get; set; } = true;
        
        /// <summary>
        /// Whether to include unit tests.
        /// </summary>
        public bool IncludeTests { get; set; } = false;
        
        /// <summary>
        /// Whether to include documentation.
        /// </summary>
        public bool IncludeDocumentation { get; set; } = true;
        
        /// <summary>
        /// Custom WebAssembly settings for optimization.
        /// </summary>
        public Dictionary<string, object> WebAssemblySettings { get; set; } = new Dictionary<string, object>();
    }
} 