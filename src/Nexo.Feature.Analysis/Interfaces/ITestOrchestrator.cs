using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Interface for intelligent test orchestration with parallel execution and dependency management.
    /// This interface is split into partial files for better organization:
    /// - ITestOrchestrator.Core.cs - Core interface methods
    /// - ITestOrchestrator.Options.cs - Options classes (TestOrchestrationOptions, ParallelExecutionOptions, etc.)
    /// - ITestOrchestrator.Results.cs - Result classes (TestOrchestrationResult, ParallelExecutionResult, etc.)
    /// - ITestOrchestrator.Plans.cs - Plan classes (TestExecutionPlan, TestExecutionPhase)
    /// - ITestOrchestrator.Metrics.cs - Metrics classes (ParallelExecutionMetrics, ResourceUtilization)
    /// - ITestOrchestrator.Support.cs - Support classes and enums (TestExecutionResult, TestDependencyRule, etc.)
    /// </summary>
    public partial interface ITestOrchestrator
    {
        // This interface is split into partial files for better organization
        // All functionality has been moved to the respective partial files listed above
    }
}
