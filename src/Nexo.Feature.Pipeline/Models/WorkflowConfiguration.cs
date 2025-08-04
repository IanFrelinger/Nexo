using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Configuration for development workflows including setup, analyze, test, and deploy.
    /// </summary>
    public class WorkflowConfiguration
    {
        /// <summary>
        /// Unique identifier for the workflow configuration.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the workflow configuration.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the workflow.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Type of workflow (setup, analyze, test, deploy).
        /// </summary>
        public WorkflowType Type { get; set; }

        /// <summary>
        /// Version of the workflow configuration.
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Steps to execute in the workflow.
        /// </summary>
        public List<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();

        /// <summary>
        /// Environment-specific configurations.
        /// </summary>
        public Dictionary<string, object> EnvironmentVariables { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Dependencies required for this workflow.
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Timeout for the entire workflow in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 3600; // 1 hour default

        /// <summary>
        /// Whether to continue execution if a step fails.
        /// </summary>
        public bool ContinueOnFailure { get; set; } = false;

        /// <summary>
        /// Configuration for retrying failed steps.
        /// </summary>
        public RetryConfiguration RetryConfig { get; set; } = new RetryConfiguration();

        /// <summary>
        /// Setup commands for the workflow.
        /// </summary>
        public List<string> SetupCommands { get; set; } = new List<string>();

        /// <summary>
        /// Analysis commands for the workflow.
        /// </summary>
        public List<string> AnalysisCommands { get; set; } = new List<string>();

        /// <summary>
        /// Test commands for the workflow.
        /// </summary>
        public List<string> TestCommands { get; set; } = new List<string>();

        /// <summary>
        /// Deploy commands for the workflow.
        /// </summary>
        public List<string> DeployCommands { get; set; } = new List<string>();

        /// <summary>
        /// When the configuration was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modified timestamp.
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Types of development workflows.
    /// </summary>
    public enum WorkflowType
    {
        /// <summary>
        /// Setup workflow for initial project configuration.
        /// </summary>
        Setup,

        /// <summary>
        /// Analysis workflow for code analysis and quality checks.
        /// </summary>
        Analyze,

        /// <summary>
        /// Testing workflow for running tests and generating reports.
        /// </summary>
        Test,

        /// <summary>
        /// Deployment workflow for building and deploying applications.
        /// </summary>
        Deploy
    }

    /// <summary>
    /// Individual step within a workflow.
    /// </summary>
    public class WorkflowStep
    {
        /// <summary>
        /// Unique identifier for the step.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the step.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of what the step does.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Type of step (command, script, pipeline, etc.).
        /// </summary>
        public StepType Type { get; set; }

        /// <summary>
        /// Command or action to execute.
        /// </summary>
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// Arguments for the command.
        /// </summary>
        public List<string> Arguments { get; set; } = new List<string>();

        /// <summary>
        /// Working directory for the step.
        /// </summary>
        public string WorkingDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Environment variables specific to this step.
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Dependencies for this step (other step IDs).
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Timeout for this step in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 300; // 5 minutes default

        /// <summary>
        /// Whether this step is required for workflow success.
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// Conditions that must be met for this step to execute.
        /// </summary>
        public List<StepCondition> Conditions { get; set; } = new List<StepCondition>();

        /// <summary>
        /// Expected exit codes for success.
        /// </summary>
        public List<int> ExpectedExitCodes { get; set; } = new List<int> { 0 };
    }

    /// <summary>
    /// Types of workflow steps.
    /// </summary>
    public enum StepType
    {
        /// <summary>
        /// Command line execution.
        /// </summary>
        Command,

        /// <summary>
        /// Script execution.
        /// </summary>
        Script,

        /// <summary>
        /// Pipeline execution.
        /// </summary>
        Pipeline,

        /// <summary>
        /// File operation.
        /// </summary>
        FileOperation,

        /// <summary>
        /// HTTP request.
        /// </summary>
        HttpRequest,

        /// <summary>
        /// Custom action.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Condition that must be met for a step to execute.
    /// </summary>
    public class StepCondition
    {
        /// <summary>
        /// Type of condition.
        /// </summary>
        public ConditionType Type { get; set; }

        /// <summary>
        /// Value to compare against.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Operator for comparison.
        /// </summary>
        public string Operator { get; set; } = "equals";

        /// <summary>
        /// Whether the condition should be negated.
        /// </summary>
        public bool Negate { get; set; } = false;
    }

    /// <summary>
    /// Types of step conditions.
    /// </summary>
    public enum ConditionType
    {
        /// <summary>
        /// File exists condition.
        /// </summary>
        FileExists,

        /// <summary>
        /// Environment variable condition.
        /// </summary>
        EnvironmentVariable,

        /// <summary>
        /// Previous step result condition.
        /// </summary>
        PreviousStepResult,

        /// <summary>
        /// Custom condition.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Configuration for retrying failed steps.
    /// </summary>
    public class RetryConfiguration
    {
        /// <summary>
        /// Maximum number of retry attempts.
        /// </summary>
        public int MaxAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retries in seconds.
        /// </summary>
        public int DelaySeconds { get; set; } = 5;

        /// <summary>
        /// Whether to use exponential backoff.
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Maximum delay between retries in seconds.
        /// </summary>
        public int MaxDelaySeconds { get; set; } = 60;
    }
}