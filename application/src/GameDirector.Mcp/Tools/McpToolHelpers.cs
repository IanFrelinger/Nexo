using System.Text.Json;

namespace GameDirector.Mcp.Tools;

internal static class McpToolHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static JsonElement Schema(params object[] properties) =>
        JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties,
            additionalProperties = true
        }, JsonOptions);
}
