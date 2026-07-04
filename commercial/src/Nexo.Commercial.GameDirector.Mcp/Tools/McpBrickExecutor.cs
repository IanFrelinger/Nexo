using System.Text.Json;
using Nexo.Brick.Contracts;
using Nexo.Core.Application.Bricks;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;

namespace GameDirector.Mcp.Tools;

/// <summary>
/// In-process brick execution for MCP tools (avoids HTTP loopback auth issues on the sidecar host).
/// </summary>
public sealed class McpBrickExecutor
{
    private readonly IBrickRegistry _bricks;

    public McpBrickExecutor(IBrickRegistry bricks) => _bricks = bricks;

    public async Task<JsonElement> ExecuteAsync(
        string brickId,
        Dictionary<string, object?> input,
        ImplementationType implementation,
        CancellationToken ct)
    {
        var brick = _bricks.GetBrick(brickId)
            ?? throw new InvalidOperationException($"DomainBrick '{brickId}' is not registered.");

        var brickInput = BrickValueSerializer.FromWireToBrickInput(
            input.ToDictionary(kv => kv.Key, kv => (object?)kv.Value ?? ""));
        BrickInputDefaults.Apply(brick, brickInput);

        var context = new Nexo.Infrastructure.Execution.ExecutionContext { AgentId = "mcp-tool" };
        var output = await brick.ExecuteAsync(brickInput, implementation, context, ct).ConfigureAwait(false);

        var wire = BrickValueSerializer.ToWireDictionary(output);
        var dto = new BrickExecuteResponseDto
        {
            Success = true,
            Summary = output.Summary,
            Output = wire.ToDictionary(kv => kv.Key, kv => kv.Value)
        };
        return JsonSerializer.SerializeToElement(dto, McpToolHelpers.JsonOptions);
    }
}
