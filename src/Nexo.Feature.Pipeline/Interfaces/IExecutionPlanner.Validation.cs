using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Execution plan validation result.
    /// </summary>
    public partial class ExecutionPlanValidationResult
    {
        /// <summary>
        /// Gets or sets the validation identifier.
        /// </summary>
        public string ValidationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the plan identifier.
        /// </summary>
        public string PlanId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the plan is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the validation errors.
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new();

        /// <summary>
        /// Gets or sets the validation warnings.
        /// </summary>
        public List<ValidationWarning> Warnings { get; set; } = new();

        /// <summary>
        /// Gets or sets the validation timestamp.
        /// </summary>
        public DateTime ValidationTimestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Validation error for execution plan.
    /// </summary>
    public partial class ValidationError
    {
        /// <summary>
        /// Gets or sets the error identifier.
        /// </summary>
        public string ErrorId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error severity.
        /// </summary>
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;

        /// <summary>
        /// Gets or sets the affected step or component.
        /// </summary>
        public string AffectedComponent { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validation warning for execution plan.
    /// </summary>
    public partial class ValidationWarning
    {
        /// <summary>
        /// Gets or sets the warning identifier.
        /// </summary>
        public string WarningId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the warning code.
        /// </summary>
        public string WarningCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the warning message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the warning severity.
        /// </summary>
        public WarningSeverity Severity { get; set; } = WarningSeverity.Warning;

        /// <summary>
        /// Gets or sets the affected step or component.
        /// </summary>
        public string AffectedComponent { get; set; } = string.Empty;
    }

    /// <summary>
    /// Error severity levels.
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// Information level.
        /// </summary>
        Information,

        /// <summary>
        /// Warning level.
        /// </summary>
        Warning,

        /// <summary>
        /// Error level.
        /// </summary>
        Error,

        /// <summary>
        /// Critical level.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Warning severity levels.
    /// </summary>
    public enum WarningSeverity
    {
        /// <summary>
        /// Information level.
        /// </summary>
        Information,

        /// <summary>
        /// Warning level.
        /// </summary>
        Warning,

        /// <summary>
        /// High warning level.
        /// </summary>
        HighWarning
    }
}
