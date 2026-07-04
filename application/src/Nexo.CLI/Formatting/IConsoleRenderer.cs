using System.Text.Json;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Agent.Models;
using Nexo.Core.Application.Common.Models;
using Nexo.Orchestration.Coordination;
using Nexo.Orchestration.Coordination.Conflicts;
using Nexo.Orchestration.Metrics;

namespace Nexo.CLI.Formatting;

/// <summary>
/// Host-specific console renderer for CLI output.
/// 
/// Provides methods for rendering various types of output to the console:
/// - Success/error messages
/// - Progress indicators
/// - Analysis, validation, and agent execution results
/// - Orchestration results, escalations, and conflicts
/// - Performance reports, metrics, and traces
/// - JSON output and tables
/// 
/// Used by CLI commands to provide consistent, formatted output.
/// </summary>
public interface IConsoleRenderer
{
    /// <summary>Writes a success message to standard output.</summary>
    void RenderSuccess(string message);

    /// <summary>Writes an error message to standard error.</summary>
    void RenderError(string message);

    /// <summary>Writes an error message with optional code and remediation suggestion.</summary>
    void RenderErrorWithCode(string message, string? errorCode, string? suggestion = null);

    /// <summary>Writes a progress start marker to standard output.</summary>
    void RenderProgressStart(string message);

    /// <summary>Writes a progress completion marker to standard output.</summary>
    void RenderProgressComplete(string message);

    /// <summary>Writes a structured progress report to standard output.</summary>
    void RenderProgress(ProgressReport report);

    /// <summary>Renders an analysis result as JSON or human-readable text.</summary>
    void RenderAnalysisResult(AnalysisResult result, bool json);

    /// <summary>Renders a validation result as JSON or human-readable text.</summary>
    void RenderValidationResult(ValidationResult result, bool json);

    /// <summary>Renders an agent execution result as JSON or human-readable text.</summary>
    void RenderAgentResult(AgentExecutionResult result, bool json);

    /// <summary>Renders a list of registered agents as JSON or human-readable text.</summary>
    void RenderAgentList(IReadOnlyList<AgentMetadata> agents, bool json);

    /// <summary>Renders an orchestration result summary to standard output.</summary>
    void RenderOrchestrationResult(OrchestrationResult result);

    /// <summary>Renders escalation summaries, optionally including verbose detail.</summary>
    void RenderEscalations(IReadOnlyList<Escalation> escalations, bool verbose);

    /// <summary>Renders a single escalation, optionally including verbose detail.</summary>
    void RenderEscalationDetails(Escalation escalation, bool verbose);

    /// <summary>Renders detected coordination conflicts, optionally including verbose detail.</summary>
    void RenderConflicts(IReadOnlyList<Conflict> conflicts, bool verbose);

    /// <summary>Renders a performance report, optionally including verbose detail.</summary>
    void RenderPerformanceReport(PerformanceReport report, bool verbose);

    /// <summary>Renders agent metrics, optionally including verbose detail.</summary>
    void RenderAgentMetrics(AgentMetrics metrics, bool verbose);

    /// <summary>Renders distributed trace spans filtered by correlation metadata.</summary>
    void RenderTraces(IReadOnlyList<TraceSpan> traces, string? correlationId, string? operationName, bool verbose);

    /// <summary>Serializes arbitrary data as JSON to standard output.</summary>
    void RenderJson(object data);

    /// <summary>Renders a tabular view of homogeneous items to standard output.</summary>
    void RenderTable<T>(IEnumerable<T> items);
}
