using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Playtest.Models;
using System.Text.Json;

namespace Nexo.Orchestration.Agents.Playtest;

/// <summary>
/// Priority levels for iteration.
/// 
/// Defines priority levels:
/// - Low: Low priority iteration
/// - Normal: Normal priority iteration
/// - High: High priority iteration
/// - Critical: Critical priority iteration
/// 
/// Used by IterationRecommendation to indicate iteration priority.
/// </summary>
public enum IterationPriority { Low, Normal, High, Critical }
