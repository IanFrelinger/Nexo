using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Services.AI.ModelFineTuning.Models
{
    /// <summary>
    /// Fine-tuning request
    /// </summary>
    public partial class FineTuningRequest
    {
        public string BaseModelId { get; set; } = string.Empty;
        public FineTuningData Data { get; set; } = new();
        public int Epochs { get; set; } = 3;
        public double LearningRate { get; set; } = 0.0001;
        public int BatchSize { get; set; } = 4;
        public string CustomInstructions { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Fine-tuning data
    /// </summary>
    public partial class FineTuningData
    {
        public List<FineTuningSample> Samples { get; set; } = new();
        public string DataFormat { get; set; } = "jsonl";
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Fine-tuning sample
    /// </summary>
    public partial class FineTuningSample
    {
        public string Input { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Fine-tuning session
    /// </summary>
    public partial class FineTuningSession
    {
        public string SessionId { get; set; } = string.Empty;
        public FineTuningRequest Request { get; set; } = new();
        public FineTuningStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Progress { get; set; }
        public string? ErrorMessage { get; set; }
        public string? FineTunedModelPath { get; set; }
        public FineTuningMetrics Metrics { get; set; } = new();
    }

    /// <summary>
    /// Fine-tuning metrics
    /// </summary>
    public partial class FineTuningMetrics
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? TotalDuration { get; set; }
        public string BaseModelId { get; set; } = string.Empty;
        public int DataSize { get; set; }
        public int TargetEpochs { get; set; }
        public int CurrentEpoch { get; set; }
        public double LearningRate { get; set; }
        public double TrainingLoss { get; set; } = 1.0;
        public double ValidationLoss { get; set; } = 1.0;
        public double Accuracy { get; set; } = 0.0;
        public DateTime LastUpdateTime { get; set; }
    }

    /// <summary>
    /// Fine-tuning status
    /// </summary>
    public enum FineTuningStatus
    {
        Initializing,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Fine-tuning validation result
    /// </summary>
    public partial class FineTuningValidationResult
    {
        public bool IsValid { get; set; }
        public DateTime ValidationTime { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Validation issue
    /// </summary>
    public partial class ValidationIssue
    {
        public ValidationIssueType Type { get; set; }
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Line { get; set; }
    }

    /// <summary>
    /// Validation issue types
    /// </summary>
    public enum ValidationIssueType
    {
        DataQuality,
        DataFormat,
        DataDiversity,
        ValidationError
    }

    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum ValidationSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}
