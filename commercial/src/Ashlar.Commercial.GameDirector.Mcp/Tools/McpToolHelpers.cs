using System.Text.Json;

namespace GameDirector.Mcp.Tools;

/// <summary>Mcp tool helpers.</summary>
internal static class McpToolHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Schema.</summary>
    /// <param name="properties">Properties.</param>
    public static JsonElement Schema(params object[] properties) =>
        JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties,
            additionalProperties = true
        }, JsonOptions);
}
