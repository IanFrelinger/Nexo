using System.Text.Json;
using Nexo.Abstractions;
using Nexo.Policies;
using Nexo.Policies.Dev;
using Nexo.Runtime;
using Nexo.Tools.Dev;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline;

/// <summary>
/// Runtime wrapper around the toolbox + policy engine (framework primitives).
/// Pipeline commands use this to invoke tools in a consistent, policy-guarded way.
/// </summary>
public sealed class SelfExtendToolRuntime
{
    private readonly CapabilityRegistry _tools;
    private readonly PolicyEngine _policies;

    public SelfExtendToolRuntime()
    {
        _tools = new CapabilityRegistry();
        _tools.Register(new RepoFsWriteTool());
        _tools.Register(new RepoFsSearchReplaceTool());
        _tools.Register(new DotnetBuildTool());
        _tools.Register(new DotnetTestTool());

        _policies = new PolicyEngine(new IPolicy[]
        {
            new OutputPathSandboxed(),
            new PathAllowlist(),
            new MaxWriteSize(),
            new PerfHeadroom(TimeSpan.FromMinutes(10))
        });
    }

    public async Task<ToolResult> InvokeAsync(SelfExtendContext ctx, string toolId, object args, CancellationToken ct)
    {
        var snapshot = new WorldSnapshot(ctx.Iteration, new Dictionary<string, object?>
        {
            ["RepoRoot"] = ctx.RepoRoot,
            ["OutputRoot"] = ctx.OutputRoot
        });

        var call = new ToolCall(toolId, JsonSerializer.SerializeToElement(args));
        if (!_policies.Approve(call, snapshot, out var reason))
        {
            throw new InvalidOperationException($"Policy denied tool call '{toolId}': {reason}");
        }

        return await _tools.InvokeAsync(call, snapshot, ct);
    }
}

