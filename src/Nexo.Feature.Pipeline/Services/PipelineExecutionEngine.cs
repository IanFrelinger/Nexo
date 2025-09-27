// This file has been refactored into specialized pipeline execution engine classes:
// - Core/PipelineExecutionEngine.cs - Main orchestrator for pipeline execution
// - Planning/ExecutionPlanGenerator.cs - Execution plan generation logic
// - Validation/DependencyValidator.cs - Dependency validation logic
// - Orchestration/ExecutionOrchestrator.cs - Execution orchestration logic
// - Monitoring/MetricsCollector.cs - Metrics collection and management

// Re-export all classes for backward compatibility
using Nexo.Feature.Pipeline.Services.Execution.Core;
using Nexo.Feature.Pipeline.Services.Execution.Planning;
using Nexo.Feature.Pipeline.Services.Execution.Validation;
using Nexo.Feature.Pipeline.Services.Execution.Orchestration;
using Nexo.Feature.Pipeline.Services.Execution.Monitoring;

namespace Nexo.Feature.Pipeline.Services
{
    // All pipeline execution engine classes are now available through the specialized files above
    // This maintains backward compatibility while providing focused, maintainable classes
}
