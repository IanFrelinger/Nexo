using System.Text.Json;

namespace Ashlar.Orchestration.Communication.Models;

/// <summary>
/// Response to a DataRequest.
/// 
/// Contains the original request message ID and the requested data.
/// Used to respond to DataRequest messages.
/// </summary>
public sealed record DataResponse : AgentMessage
{
    public DataResponse()
    {
        MessageType = "DataResponse";
    }

    /// <summary>
    /// ID of the original request message.
    /// </summary>
    public required string RequestMessageId { get; init; }

    /// <summary>
    /// The requested data.
    /// </summary>
    public required object Data { get; init; }
}
