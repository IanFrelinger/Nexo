using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Feature.Web.Models;
using Nexo.Feature.Web.Enums;

namespace Nexo.Feature.Web.Interfaces
{
    /// <summary>
    /// Interface for WebAssembly optimization services.
    /// </summary>
    public interface IWebAssemblyOptimizer
    {
        /// <summary>
        /// Optimizes WebAssembly code based on the provided configuration.
        /// </summary>
        /// <param name="sourceCode">The source code to optimize.</param>
        /// <param name="config">The optimization configuration.</param>
        /// <returns>The optimization result with metrics.</returns>
        Task<WebAssemblyOptimizationResult> OptimizeAsync(string sourceCode, WebAssemblyConfig config);
        
        /// <summary>
        /// Analyzes the performance characteristics of the code.
        /// </summary>
        /// <param name="sourceCode">The source code to analyze.</param>
        /// <returns>Performance analysis results.</returns>
        Task<WebAssemblyPerformanceAnalysis> AnalyzePerformanceAsync(string sourceCode);
        
        /// <summary>
        /// Estimates bundle size for the given code.
        /// </summary>
        /// <param name="sourceCode">The source code to analyze.</param>
        /// <param name="config">The optimization configuration.</param>
        /// <returns>Bundle size estimates.</returns>
        Task<WebAssemblyBundleAnalysis> EstimateBundleSizeAsync(string sourceCode, WebAssemblyConfig config);
        
        /// <summary>
        /// Gets available optimization strategies.
        /// </summary>
        /// <returns>List of available optimization strategies.</returns>
        IEnumerable<WebAssemblyOptimization> GetAvailableOptimizations();
    }
    
    /// <summary>
    /// Result of WebAssembly optimization.
    /// </summary>
    public class WebAssemblyOptimizationResult
    {
        public bool Success { get; set; }
        public string OptimizedCode { get; set; } = string.Empty;
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public List<string> Warnings { get; set; } = new List<string>();
        public TimeSpan OptimizationTime { get; set; }
    }
    
    /// <summary>
    /// Performance analysis result for WebAssembly code.
    /// </summary>
    public class WebAssemblyPerformanceAnalysis
    {
        public Dictionary<string, double> PerformanceMetrics { get; set; } = new Dictionary<string, double>();
        public List<string> PerformanceRecommendations { get; set; } = new List<string>();
        public Dictionary<string, object> DetailedAnalysis { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Bundle analysis result for WebAssembly code.
    /// </summary>
    public class WebAssemblyBundleAnalysis
    {
        public Dictionary<string, long> BundleSizes { get; set; } = new Dictionary<string, long>();
        public Dictionary<string, double> CompressionRatios { get; set; } = new Dictionary<string, double>();
        public List<string> SizeOptimizationSuggestions { get; set; } = new List<string>();
    }
} 