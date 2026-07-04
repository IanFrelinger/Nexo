namespace Nexo.Core.Application.Pipelines.Models;

/// <summary>
/// Directed edge between stage identifiers.
/// </summary>
/// <param name="FromStageId">Source stage identifier.</param>
/// <param name="ToStageId">Destination stage identifier.</param>
public sealed record PipelineEdge(string FromStageId, string ToStageId);
