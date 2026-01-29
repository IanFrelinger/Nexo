using System.Text.Json;
using Nexo.Abstractions;
using Nexo.CLI.Commands.BackgroundAgent;
using Nexo.Policies;
using Nexo.Policies.Dev;
using Nexo.Runtime;
using Nexo.Tools.Dev;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline;

/// <summary>
/// Runtime wrapper around the toolbox + policy engine (framework primitives).
/// Pipeline commands use this to invoke tools in a consistent, policy-guarded way.
/// Uses RepoFsToolboxFactory for minimal repo-fs tools, then adds build/test/run/analyze tools and extra policies.
/// </summary>
public sealed class SelfExtendToolRuntime
{
    private readonly CapabilityRegistry _tools;
    private readonly PolicyEngine _policies;

    public SelfExtendToolRuntime()
    {
        var (tools, _) = RepoFsToolboxFactory.CreateMinimal();
        tools.Register(new DotnetBuildTool());
        tools.Register(new DotnetTestTool());
        tools.Register(new DotnetRunTool());
        tools.Register(new RoslynAnalyzeTool());
        _tools = tools;

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
        var snapshot = WorldSnapshot.ForRepo(ctx.RepoRoot, ctx.OutputRoot, ctx.Iteration);

        var call = new ToolCall(toolId, JsonSerializer.SerializeToElement(args));
        if (!_policies.Approve(call, snapshot, out var reason))
        {
            throw new InvalidOperationException($"Policy denied tool call '{toolId}': {reason}");
        }

        return await _tools.InvokeAsync(call, snapshot, ct);
    }
}

