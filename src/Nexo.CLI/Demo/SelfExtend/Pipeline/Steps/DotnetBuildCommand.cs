using System.Text.Json;
using Nexo.Core.Application.Interfaces;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline.Steps;

public sealed class DotnetBuildCommand : ICommand<SelfExtendContext, SelfExtendContext>
{
    private readonly SelfExtendToolRuntime _rt;

    public DotnetBuildCommand(SelfExtendToolRuntime rt) => _rt = rt;

    public async ValueTask<SelfExtendContext> ExecuteAsync(SelfExtendContext input, CancellationToken ct)
    {
        var result = await _rt.InvokeAsync(input, "dotnet.build", new { root = input.RepoRoot }, ct);
        input.History.Add(new { step = "dotnet_build", delta = result.Delta.Log });
        return input;
    }
}

