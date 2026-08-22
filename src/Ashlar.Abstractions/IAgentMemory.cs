using System.Text.Json;

namespace Ashlar.Abstractions;

/// <summary>
/// Interface for agent event storage and retrieval.
///
/// Agents can write EventRecords and query them using filters.
/// Provides memory capabilities for agents.
/// </summary>
public interface IAgentMemory
{
    /// <summary>Appends an event record to this agent's memory store.</summary>
    /// <param name="e">Event to persist.</param>
    void Write(EventRecord e);

    /// <summary>
    /// Retrieves up to <paramref name="k"/> recent events matching the filter expression.
    /// </summary>
    /// <param name="filter">Filter expression interpreted by the memory implementation.</param>
    /// <param name="k">Maximum number of records to return.</param>
    IReadOnlyList<EventRecord> Query(string filter, int k);
}
