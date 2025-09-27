using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Entities.Iteration.Enums;

namespace Nexo.Core.Domain.Entities.Iteration.Requirements
{
    /// <summary>
    /// Iteration requirements for strategy selection
    /// </summary>
    public record IterationRequirements
    {
        /// <summary>
        /// Whether to prioritize CPU efficiency
        /// </summary>
        public bool PrioritizeCpu { get; init; } = false;
        
        /// <summary>
        /// Whether to prioritize memory efficiency
        /// </summary>
        public bool PrioritizeMemory { get; init; } = false;
        
        /// <summary>
        /// Whether parallelization is required
        /// </summary>
        public bool RequiresParallelization { get; init; } = false;
        
        /// <summary>
        /// Whether ordering must be preserved
        /// </summary>
        public bool RequiresOrdering { get; init; } = true;
        
        /// <summary>
        /// Whether side effects are allowed
        /// </summary>
        public bool AllowSideEffects { get; init; } = true;
        
        /// <summary>
        /// Maximum degree of parallelism
        /// </summary>
        public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
        
        /// <summary>
        /// Timeout for the operation
        /// </summary>
        public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
        
        /// <summary>
        /// Convert to PerformanceRequirements
        /// </summary>
        public Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements ToPerformanceRequirements()
        {
            return new Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements
            {
                MaxExecutionTimeMs = (int)Timeout.TotalMilliseconds,
                MaxMemoryUsageMB = PrioritizeMemory ? 50 : 100,
                RequiresRealTime = PrioritizeCpu,
                PreferParallel = RequiresParallelization,
                MemoryCritical = PrioritizeMemory
            };
        }
    }

    /// <summary>
    /// Code generation context for iteration strategies
    /// </summary>
    public record CodeGenerationContext
    {
        /// <summary>
        /// Target platform for code generation
        /// </summary>
        public PlatformTarget PlatformTarget { get; init; } = PlatformTarget.DotNet;
        
        /// <summary>
        /// Variable name for the collection
        /// </summary>
        public string CollectionVariableName { get; init; } = "items";
        
        /// <summary>
        /// Collection name (alias for CollectionVariableName)
        /// </summary>
        public string CollectionName { get; init; } = "items";
        
        /// <summary>
        /// Variable name for the item
        /// </summary>
        public string ItemVariableName { get; init; } = "item";
        
        /// <summary>
        /// Item name (alias for ItemVariableName)
        /// </summary>
        public string ItemName { get; init; } = "item";
        
        /// <summary>
        /// Action to perform on each item
        /// </summary>
        public string ActionCode { get; init; } = "// Process item";
        
        /// <summary>
        /// Iteration body template (alias for ActionCode)
        /// </summary>
        public string IterationBodyTemplate { get; init; } = "// Process item";
        
        /// <summary>
        /// Whether to include null checks
        /// </summary>
        public bool IncludeNullChecks { get; init; } = true;
        
        /// <summary>
        /// Whether the context has a Where clause
        /// </summary>
        public bool HasWhere { get; init; } = false;
        
        /// <summary>
        /// Whether the context has a Select clause
        /// </summary>
        public bool HasSelect { get; init; } = false;
        
        /// <summary>
        /// Predicate template for Where clauses
        /// </summary>
        public string PredicateTemplate { get; init; } = "x => true";
        
        /// <summary>
        /// Transform template for Select clauses
        /// </summary>
        public string TransformTemplate { get; init; } = "x => x";
        
        /// <summary>
        /// Action template for ForEach operations
        /// </summary>
        public string ActionTemplate { get; init; } = "x => { /* action */ }";
        
        /// <summary>
        /// Whether to include bounds checking
        /// </summary>
        public bool IncludeBoundsChecking { get; init; } = true;
        
        /// <summary>
        /// Whether the context requires async processing
        /// </summary>
        public bool HasAsync { get; init; } = false;
        
        /// <summary>
        /// Performance requirements
        /// </summary>
        public Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements PerformanceRequirements { get; init; } = new();
        
        /// <summary>
        /// Additional context for code generation
        /// </summary>
        public Dictionary<string, object> AdditionalContext { get; init; } = new();
    }

    /// <summary>
    /// Code generation context for iteration strategies
    /// </summary>
    public record CodeGeneration
    {
        /// <summary>
        /// Target platform for code generation
        /// </summary>
        public PlatformTarget PlatformTarget { get; init; } = PlatformTarget.DotNet;
        
        /// <summary>
        /// Variable name for the collection
        /// </summary>
        public string CollectionVariableName { get; init; } = "items";
        
        /// <summary>
        /// Variable name for the item
        /// </summary>
        public string ItemVariableName { get; init; } = "item";
        
        /// <summary>
        /// Action to perform on each item
        /// </summary>
        public string ActionCode { get; init; } = "// Process item";
        
        /// <summary>
        /// Whether to include null checks
        /// </summary>
        public bool IncludeNullChecks { get; init; } = true;
        
        /// <summary>
        /// Whether the context has a Where clause
        /// </summary>
        public bool HasWhere { get; init; } = false;
        
        /// <summary>
        /// Whether the context has a Select clause
        /// </summary>
        public bool HasSelect { get; init; } = false;
        
        /// <summary>
        /// Predicate template for Where clauses
        /// </summary>
        public string PredicateTemplate { get; init; } = "x => true";
        
        /// <summary>
        /// Transform template for Select clauses
        /// </summary>
        public string TransformTemplate { get; init; } = "x => x";
        
        /// <summary>
        /// Action template for ForEach operations
        /// </summary>
        public string ActionTemplate { get; init; } = "x => { /* action */ }";
        
        /// <summary>
        /// Whether to include bounds checking
        /// </summary>
        public bool IncludeBoundsChecking { get; init; } = true;
        
        /// <summary>
        /// Whether the context requires async processing
        /// </summary>
        public bool HasAsync { get; init; } = false;
        
        /// <summary>
        /// Performance requirements
        /// </summary>
        public Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements PerformanceRequirements { get; init; } = new();
        
        /// <summary>
        /// Additional context for code generation
        /// </summary>
        public Dictionary<string, object> AdditionalContext { get; init; } = new();
    }
}
