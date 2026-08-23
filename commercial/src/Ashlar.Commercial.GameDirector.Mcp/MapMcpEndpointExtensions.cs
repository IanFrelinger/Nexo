using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GameDirector.Mcp;

/// <summary>Map mcp endpoint extensions.</summary>
public static class MapMcpEndpointExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapMcpEndpoint(this IEndpointRouteBuilder app, string path = "/mcp")
    {
        app.MapPost(path, HandleMcpAsync)
            .WithName("GameDirectorMcp")
            .WithTags("MCP")
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> HandleMcpAsync(HttpContext http, McpToolRegistry registry, CancellationToken ct)
    {
        using var doc = await JsonDocument.ParseAsync(http.Request.Body, cancellationToken: ct).ConfigureAwait(false);
        var root = doc.RootElement;
        if (!root.TryGetProperty("method", out var methodEl))
            return Results.BadRequest(new { error = "missing method" });

        var method = methodEl.GetString() ?? "";
        var id = root.TryGetProperty("id", out var idEl) ? idEl.Clone() : default;
        var parameters = root.TryGetProperty("params", out var paramsEl) ? paramsEl : default;

        object? result = method switch
        {
            "initialize" => new
            {
                protocolVersion = "2024-11-05",
                serverInfo = new { name = "ashlar-game-director", version = "0.1.0" },
                capabilities = new { tools = new { } }
            },
            "tools/list" => new { tools = registry.ListTools().Select(t => new
            {
                name = t.Name,
                description = t.Description,
                inputSchema = t.InputSchema
            }) },
            "tools/call" => await HandleToolsCallAsync(registry, parameters, ct).ConfigureAwait(false),
            "ping" => new { },
            _ => new { error = $"unsupported method: {method}" }
        };

        var response = new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id.ValueKind == JsonValueKind.Undefined ? null : id,
            ["result"] = result
        };
        return Results.Json(response, JsonOptions);
    }

    private static async Task<object> HandleToolsCallAsync(
        McpToolRegistry registry,
        JsonElement parameters,
        CancellationToken ct)
    {
        var name = parameters.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        var args = parameters.TryGetProperty("arguments", out var a) ? a : default;
        try
        {
            var content = await registry.CallAsync(name, args.ValueKind == JsonValueKind.Undefined ? null : args, ct)
                .ConfigureAwait(false);
            return new
            {
                content = new[]
                {
                    new { type = "text", text = content.GetRawText() }
                }
            };
        }
        catch (Exception ex)
        {
            return new
            {
                isError = true,
                content = new[] { new { type = "text", text = ex.Message } }
            };
        }
    }
}
