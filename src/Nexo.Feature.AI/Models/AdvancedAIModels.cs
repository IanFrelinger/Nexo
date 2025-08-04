using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Advanced model request with detailed requirements and processing options.
    /// </summary>
    public class AdvancedModelRequest
    {
        /// <summary>
        /// Gets or sets the input content for the model.
        /// </summary>
        public string Input { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of task to perform.
        /// </summary>
        public string TaskType { get; set; } = "general";

        /// <summary>
        /// Gets or sets the complexity level of the task (1-5).
        /// </summary>
        public int ComplexityLevel { get; set; } = 1;

        /// <summary>
        /// Gets or sets the required programming languages.
        /// </summary>
        public List<string> RequiredLanguages { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the maximum number of tokens to generate.
        /// </summary>
        public int MaxTokens { get; set; } = 2048;

        /// <summary>
        /// Gets or sets the temperature for response generation.
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Gets or sets additional metadata for the request.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets post-processing options to apply to the response.
        /// </summary>
        public List<PostProcessingOption> PostProcessingOptions { get; set; } = new List<PostProcessingOption>();

        /// <summary>
        /// Gets or sets the preferred model to use (if specified).
        /// </summary>
        public string PreferredModel { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to use fallback models if the preferred model fails.
        /// </summary>
        public bool UseFallback { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum cost budget for this request.
        /// </summary>
        public decimal MaxCost { get; set; } = 1.0m;

        /// <summary>
        /// Gets or sets the timeout for the request in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;
    }

    /// <summary>
    /// Advanced model response with detailed metrics and processing information.
    /// </summary>
    public class AdvancedModelResponse
    {
        /// <summary>
        /// Gets or sets the generated content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the request was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the request failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model that was used for processing.
        /// </summary>
        public string ModelUsed { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the processing time in milliseconds.
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens used.
        /// </summary>
        public long TokensUsed { get; set; }

        /// <summary>
        /// Gets or sets the cost of the request.
        /// </summary>
        public decimal Cost { get; set; }

        /// <summary>
        /// Gets or sets whether a fallback model was used.
        /// </summary>
        public bool FallbackUsed { get; set; }

        /// <summary>
        /// Gets or sets the confidence score of the response.
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the response.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the post-processing results.
        /// </summary>
        public List<PostProcessingResult> PostProcessingResults { get; set; } = new List<PostProcessingResult>();
    }

    /// <summary>
    /// Multi-model workflow definition for complex AI processing chains.
    /// </summary>
    public class MultiModelWorkflow
    {
        /// <summary>
        /// Gets or sets the workflow name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the workflow description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the workflow steps to execute.
        /// </summary>
        public List<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();

        /// <summary>
        /// Gets or sets the workflow configuration.
        /// </summary>
        public WorkflowConfiguration Configuration { get; set; } = new WorkflowConfiguration();

        /// <summary>
        /// Gets or sets the workflow metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Individual step in a multi-model workflow.
    /// </summary>
    public class WorkflowStep
    {
        /// <summary>
        /// Gets or sets the step name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the step description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the input template for this step.
        /// </summary>
        public string InputTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the task type for this step.
        /// </summary>
        public string TaskType { get; set; } = "general";

        /// <summary>
        /// Gets or sets the complexity level for this step.
        /// </summary>
        public int ComplexityLevel { get; set; } = 1;

        /// <summary>
        /// Gets or sets the required languages for this step.
        /// </summary>
        public List<string> RequiredLanguages { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the maximum tokens for this step.
        /// </summary>
        public int MaxTokens { get; set; } = 2048;

        /// <summary>
        /// Gets or sets the temperature for this step.
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Gets or sets whether this step is critical for the workflow.
        /// </summary>
        public bool IsCritical { get; set; } = false;

        /// <summary>
        /// Gets or sets the input placeholders for this step.
        /// </summary>
        public List<InputPlaceholder> InputPlaceholders { get; set; } = new List<InputPlaceholder>();

        /// <summary>
        /// Gets or sets the dependencies for this step.
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the step configuration.
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Input placeholder for workflow step input preparation.
    /// </summary>
    public class InputPlaceholder
    {
        /// <summary>
        /// Gets or sets the placeholder name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source step name.
        /// </summary>
        public string SourceStep { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the extraction method for the value.
        /// </summary>
        public string ExtractionMethod { get; set; } = "content";

        /// <summary>
        /// Gets or sets the static value for this placeholder (used when SourceStep is "initial").
        /// </summary>
        public string StaticValue { get; set; } = string.Empty;

        /// <summary>
        /// Extracts the value from a workflow step result.
        /// </summary>
        /// <param name="result">The workflow step result.</param>
        /// <returns>The extracted value.</returns>
        public string ExtractValue(WorkflowStepResult result)
        {
            switch (ExtractionMethod.ToLower())
            {
                case "content":
                    return result.Content;
                case "metadata":
                    return result.Metadata?.ToString() ?? string.Empty;
                case "processingtime":
                    return result.ProcessingTimeMs.ToString();
                case "static":
                    return StaticValue;
                default:
                    return result.Content;
            }
        }
    }

    /// <summary>
    /// Workflow configuration for multi-model workflows.
    /// </summary>
    public class WorkflowConfiguration
    {
        /// <summary>
        /// Gets or sets whether to execute steps in parallel when possible.
        /// </summary>
        public bool EnableParallelExecution { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of parallel steps.
        /// </summary>
        public int MaxParallelSteps { get; set; } = 3;

        /// <summary>
        /// Gets or sets the timeout for the entire workflow in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 300000; // 5 minutes

        /// <summary>
        /// Gets or sets whether to continue execution on non-critical step failures.
        /// </summary>
        public bool ContinueOnNonCriticalFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets the retry configuration for failed steps.
        /// </summary>
        public RetryConfiguration RetryConfiguration { get; set; } = new RetryConfiguration();

        /// <summary>
        /// Gets or sets the logging configuration.
        /// </summary>
        public LoggingConfiguration LoggingConfiguration { get; set; } = new LoggingConfiguration();
    }

    /// <summary>
    /// Retry configuration for workflow steps.
    /// </summary>
    public class RetryConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum number of retries.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay between retries in milliseconds.
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to use exponential backoff.
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Gets or sets the backoff multiplier.
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;
    }

    /// <summary>
    /// Logging configuration for workflows.
    /// </summary>
    public class LoggingConfiguration
    {
        /// <summary>
        /// Gets or sets whether to log step inputs.
        /// </summary>
        public bool LogStepInputs { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log step outputs.
        /// </summary>
        public bool LogStepOutputs { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log performance metrics.
        /// </summary>
        public bool LogPerformanceMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets the log level for workflow execution.
        /// </summary>
        public string LogLevel { get; set; } = "Information";
    }

    /// <summary>
    /// Multi-model workflow response with step results.
    /// </summary>
    public class MultiModelResponse
    {
        /// <summary>
        /// Gets or sets whether the workflow was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the workflow failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the step results.
        /// </summary>
        public List<WorkflowStepResult> StepResults { get; set; } = new List<WorkflowStepResult>();

        /// <summary>
        /// Gets or sets the workflow context.
        /// </summary>
        public WorkflowContext WorkflowContext { get; set; } = new WorkflowContext();

        /// <summary>
        /// Gets or sets the total processing time in milliseconds.
        /// </summary>
        public long TotalProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the total cost of the workflow.
        /// </summary>
        public decimal TotalCost { get; set; }

        /// <summary>
        /// Gets or sets the workflow metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of a workflow step execution.
    /// </summary>
    public class WorkflowStepResult
    {
        /// <summary>
        /// Gets or sets the step name.
        /// </summary>
        public string StepName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the step was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the generated content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message if the step failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model that was used.
        /// </summary>
        public string ModelUsed { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the processing time in milliseconds.
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens used.
        /// </summary>
        public long TokensUsed { get; set; }

        /// <summary>
        /// Gets or sets the cost of the step.
        /// </summary>
        public decimal Cost { get; set; }

        /// <summary>
        /// Gets or sets the step metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the retry count for this step.
        /// </summary>
        public int RetryCount { get; set; }
    }

    /// <summary>
    /// Model optimization analysis result.
    /// </summary>
    public class ModelOptimizationResult
    {
        /// <summary>
        /// Gets or sets whether the analysis was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the analysis failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the performance analysis patterns.
        /// </summary>
        public List<PerformancePattern> PerformanceAnalysis { get; set; } = new List<PerformancePattern>();

        /// <summary>
        /// Gets or sets the identified bottlenecks.
        /// </summary>
        public List<string> Bottlenecks { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the optimization recommendations.
        /// </summary>
        public List<OptimizationRecommendation> Recommendations { get; set; } = new List<OptimizationRecommendation>();

        /// <summary>
        /// Gets or sets the analysis timestamp.
        /// </summary>
        public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the analysis metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Post-processing option for advanced model responses.
    /// </summary>
    public class PostProcessingOption
    {
        /// <summary>
        /// Gets or sets the post-processing type.
        /// </summary>
        public PostProcessingType Type { get; set; }

        /// <summary>
        /// Gets or sets the parameters for the post-processing.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the priority of this post-processing option.
        /// </summary>
        public int Priority { get; set; } = 1;
    }

    /// <summary>
    /// Post-processing result.
    /// </summary>
    public class PostProcessingResult
    {
        /// <summary>
        /// Gets or sets the post-processing type.
        /// </summary>
        public PostProcessingType Type { get; set; }

        /// <summary>
        /// Gets or sets whether the post-processing was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if post-processing failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the processing time in milliseconds.
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the post-processing metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Post-processing types.
    /// </summary>
    public enum PostProcessingType
    {
        /// <summary>
        /// Content formatting.
        /// </summary>
        Formatting,

        /// <summary>
        /// Content validation.
        /// </summary>
        Validation,

        /// <summary>
        /// Content enhancement.
        /// </summary>
        Enhancement,

        /// <summary>
        /// Content summarization.
        /// </summary>
        Summarization,

        /// <summary>
        /// Content translation.
        /// </summary>
        Translation,

        /// <summary>
        /// Content analysis.
        /// </summary>
        Analysis
    }

    // Supporting classes for AdvancedModelOrchestrator
    public class ModelCandidate
    {
        public string ModelName { get; set; } = string.Empty;
        public double Score { get; set; }
    }

    public class ModelPerformanceMetrics
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public long TotalProcessingTimeMs { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public double SuccessRate { get; set; }
        public double ErrorRate { get; set; }
        public long TotalTokens { get; set; }
        public double AverageTokensPerRequest { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageCostPerToken { get; set; }
    }

    public class ModelCapabilityProfile
    {
        public string[] SupportedLanguages { get; set; } = Array.Empty<string>();
        public string[] SupportedTasks { get; set; } = Array.Empty<string>();
        public int MaxComplexity { get; set; }
        public int MaxTokens { get; set; }
        public decimal CostPerToken { get; set; }
    }

    public class ModelSelectionRule
    {
        public string Name { get; set; } = string.Empty;
        public int Priority { get; set; }
        public Func<AdvancedModelRequest, string, Task<bool>> Condition { get; set; } = null;
        public bool IsDynamic { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class PerformancePattern
    {
        public string ModelName { get; set; } = string.Empty;
        public double AverageResponseTime { get; set; }
        public double SuccessRate { get; set; }
        public decimal CostEfficiency { get; set; }
        public int RequestVolume { get; set; }
    }

    public class OptimizationRecommendation
    {
        public OptimizationType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public OptimizationPriority Priority { get; set; }
        public string EstimatedImpact { get; set; } = string.Empty;
    }

    public enum OptimizationType
    {
        Performance,
        Cost,
        Reliability,
        Security
    }

    public enum OptimizationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class WorkflowContext
    {
        public Dictionary<string, WorkflowStepResult> StepResults { get; set; } = new Dictionary<string, WorkflowStepResult>();
        public Dictionary<string, object> SharedData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Model health status information.
    /// </summary>
    public class ModelHealthStatus
    {
        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        public string ProviderName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the provider is healthy.
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Gets or sets the response time in milliseconds.
        /// </summary>
        public long ResponseTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the error rate.
        /// </summary>
        public double ErrorRate { get; set; }

        /// <summary>
        /// Gets or sets the last error message.
        /// </summary>
        public string LastError { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last check time.
        /// </summary>
        public DateTime LastCheckTime { get; set; }
    }

    /// <summary>
    /// Model optimization recommendation.
    /// </summary>
    public class ModelOptimizationRecommendation
    {
        /// <summary>
        /// Gets or sets the recommendation type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the recommendation message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expected impact.
        /// </summary>
        public string Impact { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        public int Priority { get; set; }
    }
} 