using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Nexo.Abstractions.ModelInput;

namespace Nexo.Orchestration.Agents.Planning;

/// <summary>Interactive planning question presented to the user.</summary>
public class PlanningQuestion
{
    /// <summary>Stable question identifier within the planning session.</summary>
    public required string Id { get; set; }

    /// <summary>Human-readable question text.</summary>
    public required string Text { get; set; }

    /// <summary>Planning category or topic label.</summary>
    public required string Category { get; set; }

    /// <summary>Optional predefined answer choices.</summary>
    public string[]? Options { get; set; }
}
