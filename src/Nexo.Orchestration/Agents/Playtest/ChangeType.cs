using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Playtest.Models;
using System.Text.Json;

namespace Nexo.Orchestration.Agents.Playtest;

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
