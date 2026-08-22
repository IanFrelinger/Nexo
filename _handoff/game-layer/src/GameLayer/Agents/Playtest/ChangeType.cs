using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Playtest.Models;
using System.Text.Json;

namespace Ashlar.Orchestration.Agents.Playtest;

/// <summary>
/// Types of design changes.
/// 
/// Defines change types:
/// - Modify: Change an existing element
/// - Add: Add a new element
/// - Remove: Remove an element
/// 
/// Used by DesignChangeRequest to specify the type of change.
/// </summary>
public enum ChangeType { Modify, Add, Remove }
