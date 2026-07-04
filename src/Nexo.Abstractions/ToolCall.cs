using System.Text.Json;

namespace Nexo.Abstractions;

/// <summary>
/// Represents a call to a tool with arguments.
///
/// Contains:
/// - Tool identifier
/// - Arguments as JSON
///
/// Provides a ParseArgs method to deserialize arguments to a specific type.
/// </summary>
/// <param name="Id">Identifier of the tool to call.</param>
/// <param name="Arguments">Tool arguments as a JSON element.</param>
public sealed record ToolCall(string Id, JsonElement Arguments)
{
    /// <summary>
    /// Parses the arguments JSON to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The deserialized arguments object.</returns>
    public T ParseArgs<T>() => JsonSerializer.Deserialize<T>(Arguments)!;
}
