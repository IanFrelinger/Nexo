namespace Nexo.Core.Application.Workflows;

/// <summary>Thrown when a workflow definition fails structural validation before execution.</summary>
public class WorkflowValidationException : Exception
{
    /// <summary>Validation errors that caused the workflow to be rejected.</summary>
    public List<string> Errors { get; }

    /// <summary>Creates an exception from a list of validation errors.</summary>
    /// <param name="errors">Validation failure messages.</param>
    public WorkflowValidationException(List<string> errors)
        : base($"Workflow validation failed: {string.Join(", ", errors)}")
    {
        Errors = errors;
    }
}
