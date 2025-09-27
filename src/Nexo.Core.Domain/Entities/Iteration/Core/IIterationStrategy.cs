using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Entities.Iteration.Performance;
using Nexo.Core.Domain.Entities.Iteration.Enums;
using Nexo.Core.Domain.Entities.Iteration.Context;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Domain.Entities.Iteration.Core
{
    /// <summary>
    /// Core iteration strategy abstraction for the Nexo pipeline
    /// </summary>
    public interface IIterationStrategy<T>
    {
        /// <summary>
        /// Unique identifier for this iteration strategy
        /// </summary>
        string StrategyId { get; }
        
        /// <summary>
        /// Performance characteristics of this strategy
        /// </summary>
        IterationPerformanceProfile PerformanceProfile { get; }
        
        /// <summary>
        /// Platform compatibility for this strategy
        /// </summary>
        PlatformCompatibility PlatformCompatibility { get; }
        
        /// <summary>
        /// Execute iteration with action
        /// </summary>
        void Execute(IEnumerable<T> source, Action<T> action);
        
        /// <summary>
        /// Execute iteration with transformation
        /// </summary>
        IEnumerable<TResult> Execute<TResult>(IEnumerable<T> source, Func<T, TResult> transform);
        
        /// <summary>
        /// Execute iteration with filtering and transformation
        /// </summary>
        IEnumerable<TResult> ExecuteWhere<TResult>(IEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector);
        
        /// <summary>
        /// Execute async iteration
        /// </summary>
        Task ExecuteAsync(IEnumerable<T> source, Func<T, Task> asyncAction);
        
        /// <summary>
        /// Generate code for Feature Factory
        /// </summary>
        string GenerateCode(CodeGenerationContext context);
        
        /// <summary>
        /// Check if this strategy can handle the given pipeline context
        /// </summary>
        bool CanHandle(IIterationPipelineContext context);
        
        /// <summary>
        /// Get priority score for this strategy in the given context
        /// </summary>
        int GetPriority(IIterationPipelineContext context);
        
        /// <summary>
        /// Estimate performance for given context
        /// </summary>
        Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context);
    }
}
