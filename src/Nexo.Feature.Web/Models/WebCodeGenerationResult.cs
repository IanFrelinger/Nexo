using System.Collections.Generic;
using Nexo.Feature.Web.Enums;

namespace Nexo.Feature.Web.Models
{
    /// <summary>
    /// Result model for web code generation.
    /// </summary>
    public class WebCodeGenerationResult
    {
        /// <summary>
        /// Whether the code generation was successful.
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Success or error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// The generated component code.
        /// </summary>
        public string ComponentCode { get; set; } = string.Empty;
        
        /// <summary>
        /// The generated TypeScript types (if applicable).
        /// </summary>
        public string TypeScriptTypes { get; set; } = string.Empty;
        
        /// <summary>
        /// The generated CSS/styling (if applicable).
        /// </summary>
        public string StylingCode { get; set; } = string.Empty;
        
        /// <summary>
        /// The generated unit tests (if applicable).
        /// </summary>
        public string TestCode { get; set; } = string.Empty;
        
        /// <summary>
        /// The generated documentation (if applicable).
        /// </summary>
        public string Documentation { get; set; } = string.Empty;
        
        /// <summary>
        /// WebAssembly optimization results and metrics.
        /// </summary>
        public Dictionary<string, object> WebAssemblyMetrics { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Performance metrics for the generated code.
        /// </summary>
        public Dictionary<string, double> PerformanceMetrics { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Bundle size information.
        /// </summary>
        public Dictionary<string, long> BundleSizes { get; set; } = new Dictionary<string, long>();
        
        /// <summary>
        /// Generated file paths.
        /// </summary>
        public List<string> GeneratedFiles { get; set; } = new List<string>();
        
        /// <summary>
        /// Warnings or recommendations.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
        
        /// <summary>
        /// The framework used for generation.
        /// </summary>
        public WebFrameworkType Framework { get; set; }
        
        /// <summary>
        /// The component type generated.
        /// </summary>
        public WebComponentType ComponentType { get; set; }
        
        /// <summary>
        /// The optimization strategy applied.
        /// </summary>
        public WebAssemblyOptimization Optimization { get; set; }
    }
} 