using System.Collections.Generic;
using Nexo.Feature.Web.Enums;

namespace Nexo.Feature.Web.Models
{
    /// <summary>
    /// Configuration model for WebAssembly optimization settings.
    /// </summary>
    public class WebAssemblyConfig
    {
        /// <summary>
        /// The optimization strategy to apply.
        /// </summary>
        public WebAssemblyOptimization Optimization { get; set; } = WebAssemblyOptimization.Balanced;
        
        /// <summary>
        /// Whether to enable tree shaking for unused code elimination.
        /// </summary>
        public bool EnableTreeShaking { get; set; } = true;
        
        /// <summary>
        /// Whether to enable code splitting for better caching.
        /// </summary>
        public bool EnableCodeSplitting { get; set; } = true;
        
        /// <summary>
        /// Whether to enable minification.
        /// </summary>
        public bool EnableMinification { get; set; } = true;
        
        /// <summary>
        /// Whether to enable source maps for debugging.
        /// </summary>
        public bool EnableSourceMaps { get; set; } = false;
        
        /// <summary>
        /// Target browsers for compatibility.
        /// </summary>
        public List<string> TargetBrowsers { get; set; } = new List<string>();
        
        /// <summary>
        /// Custom optimization flags.
        /// </summary>
        public Dictionary<string, object> CustomFlags { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Memory allocation settings.
        /// </summary>
        public WebAssemblyMemoryConfig Memory { get; set; } = new WebAssemblyMemoryConfig();
        
        /// <summary>
        /// Threading configuration.
        /// </summary>
        public WebAssemblyThreadingConfig Threading { get; set; } = new WebAssemblyThreadingConfig();
        
        /// <summary>
        /// SIMD (Single Instruction, Multiple Data) settings.
        /// </summary>
        public WebAssemblySimdConfig Simd { get; set; } = new WebAssemblySimdConfig();
    }
    
    /// <summary>
    /// Memory configuration for WebAssembly.
    /// </summary>
    public class WebAssemblyMemoryConfig
    {
        /// <summary>
        /// Initial memory size in pages (64KB each).
        /// </summary>
        public int InitialPages { get; set; } = 256; // 16MB
        
        /// <summary>
        /// Maximum memory size in pages.
        /// </summary>
        public int MaxPages { get; set; } = 2048; // 128MB
        
        /// <summary>
        /// Whether to enable shared memory.
        /// </summary>
        public bool EnableSharedMemory { get; set; } = false;
    }
    
    /// <summary>
    /// Threading configuration for WebAssembly.
    /// </summary>
    public class WebAssemblyThreadingConfig
    {
        /// <summary>
        /// Whether to enable threading support.
        /// </summary>
        public bool EnableThreading { get; set; } = false;
        
        /// <summary>
        /// Maximum number of threads.
        /// </summary>
        public int MaxThreads { get; set; } = 4;
        
        /// <summary>
        /// Whether to use Web Workers for threading.
        /// </summary>
        public bool UseWebWorkers { get; set; } = true;
    }
    
    /// <summary>
    /// SIMD configuration for WebAssembly.
    /// </summary>
    public class WebAssemblySimdConfig
    {
        /// <summary>
        /// Whether to enable SIMD instructions.
        /// </summary>
        public bool EnableSimd { get; set; } = true;
        
        /// <summary>
        /// SIMD instruction set to target.
        /// </summary>
        public string InstructionSet { get; set; } = "wasm_simd128";
    }
} 