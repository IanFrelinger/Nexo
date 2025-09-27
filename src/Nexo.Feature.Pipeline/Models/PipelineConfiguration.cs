// This file has been refactored into specialized configuration files:
// - Core/PipelineConfiguration.cs - Core pipeline configuration and IPipelineConfiguration implementation
// - Execution/PipelineExecutionSettings.cs - Pipeline execution settings
// - Commands/PipelineCommandConfiguration.cs - Command configuration and retry settings
// - Behaviors/PipelineBehaviorConfiguration.cs - Behavior configuration
// - Aggregators/PipelineAggregatorConfiguration.cs - Aggregator configuration and resource requirements
// - Environments/PipelineEnvironmentConfiguration.cs - Environment-specific configuration
// - Validation/PipelineValidationConfiguration.cs - Validation configuration and rules
// - Documentation/PipelineDocumentationConfiguration.cs - Documentation configuration

// Re-export all models for backward compatibility
using Nexo.Feature.Pipeline.Models.Configuration.Core;
using Nexo.Feature.Pipeline.Models.Configuration.Execution;
using Nexo.Feature.Pipeline.Models.Configuration.Commands;
using Nexo.Feature.Pipeline.Models.Configuration.Behaviors;
using Nexo.Feature.Pipeline.Models.Configuration.Aggregators;
using Nexo.Feature.Pipeline.Models.Configuration.Environments;
using Nexo.Feature.Pipeline.Models.Configuration.Validation;
using Nexo.Feature.Pipeline.Models.Configuration.Documentation;

namespace Nexo.Feature.Pipeline.Models
{
    // All configuration models are now available through the specialized files above
    // This maintains backward compatibility while providing focused, maintainable configuration classes
}
