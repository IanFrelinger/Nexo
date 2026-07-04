using System.Text.Json;

namespace Nexo.Abstractions;

/// <summary>
/// Record of an event that occurred in the agent system.
///
/// Contains:
/// - Timestamp when the event occurred
/// - Agent that generated the event
/// - Event type/category
/// - Event message/description
///
/// Stored in IAgentMemory for agent event history and querying.
/// </summary>
/// <param name="At">Timestamp when the event occurred.</param>
/// <param name="Agent">Name or ID of the agent that generated the event.</param>
/// <param name="EventType">Type or category of the event.</param>
/// <param name="Message">Event message or description.</param>
public sealed record EventRecord(DateTimeOffset At, string Agent, string EventType, string Message);
