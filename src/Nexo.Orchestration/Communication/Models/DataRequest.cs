using System.Text.Json;

namespace Nexo.Orchestration.Communication.Models;

/// <summary>
/// Message for requesting data from another agent.
/// 
/// Contains the type of data requested and optional filter criteria.
/// Used for direct agent-to-agent data requests.
/// </summary>
public sealed record DataRequest : AgentMessage
{
    public DataRequest()
    {
        MessageType = "DataRequest";
    }

    /// <summary>
    /// Type of data requested.
    /// </summary>
    public required string RequestedDataType { get; init; }

    /// <summary>
    /// Optional filter criteria.
    /// </summary>
    public JsonElement? Filter { get; init; }
}
