using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Entities.Iteration.Context
{
    /// <summary>
    /// Iteration context for strategy selection
    /// </summary>
    public record IterationContext
    {
        /// <summary>
        /// Estimated data size
        /// </summary>
        public int DataSize { get; init; }
        
        /// <summary>
        /// Estimated data size (alias for DataSize)
        /// </summary>
        public int EstimatedDataSize => DataSize;
        
        /// <summary>
        /// Iteration requirements
        /// </summary>
        public IterationRequirements Requirements { get; init; } = new();
        
        /// <summary>
        /// Runtime environment profile
        /// </summary>
        public RuntimeEnvironmentProfile EnvironmentProfile { get; init; } = new();
        
        /// <summary>
        /// Pipeline context
        /// </summary>
        public IIterationPipelineContext? PipelineContext { get; init; }
        
        /// <summary>
        /// Target platform
        /// </summary>
        public PlatformTarget TargetPlatform { get; init; } = PlatformTarget.DotNet;
        
        /// <summary>
        /// Whether the operation is CPU-bound
        /// </summary>
        public bool IsCpuBound { get; init; } = false;
        
        /// <summary>
        /// Whether the operation is I/O-bound
        /// </summary>
        public bool IsIoBound { get; init; } = false;
        
        /// <summary>
        /// Whether the operation requires async processing
        /// </summary>
        public bool RequiresAsync { get; init; } = false;
        
        /// <summary>
        /// Code generation context
        /// </summary>
        public CodeGeneration? CodeGeneration { get; init; }
    }

    /// <summary>
    /// Pipeline context interface for iteration strategies
    /// </summary>
    public interface IIterationPipelineContext
    {
        /// <summary>
        /// Unique execution identifier
        /// </summary>
        string ExecutionId { get; }
        
        /// <summary>
        /// Execution start time
        /// </summary>
        DateTime StartTime { get; }
        
        /// <summary>
        /// Shared data store
        /// </summary>
        Dictionary<string, object> SharedData { get; }
        
        /// <summary>
        /// Get a value from shared data
        /// </summary>
        T? GetValue<T>(string key, T? defaultValue = default);
        
        /// <summary>
        /// Set a value in shared data
        /// </summary>
        void SetValue<T>(string key, T value);
        
        /// <summary>
        /// Check if a key exists in shared data
        /// </summary>
        bool HasValue(string key);
        
        /// <summary>
        /// Data size for iteration
        /// </summary>
        int DataSize { get; }
        
        /// <summary>
        /// Whether parallelization is required
        /// </summary>
        bool RequiresParallelization { get; }
        
        /// <summary>
        /// Platform target
        /// </summary>
        PlatformTarget PlatformTarget { get; }
        
        /// <summary>
        /// Priority level
        /// </summary>
        int Priority { get; }
    }
}
