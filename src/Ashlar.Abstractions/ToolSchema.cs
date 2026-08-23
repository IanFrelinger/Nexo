using System.Text.Json;

namespace Ashlar.Abstractions;

/// <summary>
/// Schema describing a tool's capabilities and inputs.
///
/// Contains:
/// - Tool identifier
/// - Human-readable description
/// - JSON schema for input validation
///
/// Used by agents to discover and understand available tools.
/// </summary>
/// <param name="Id">Unique identifier for the tool.</param>
/// <param name="Description">Human-readable description of what the tool does.</param>
/// <param name="InputJsonSchema">JSON schema describing the tool's input parameters (null if no schema).</param>
public sealed record ToolSchema(string Id, string Description, string? InputJsonSchema);
