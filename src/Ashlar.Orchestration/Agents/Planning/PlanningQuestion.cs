using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Ashlar.Abstractions.ModelInput;

namespace Ashlar.Orchestration.Agents.Planning;

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
