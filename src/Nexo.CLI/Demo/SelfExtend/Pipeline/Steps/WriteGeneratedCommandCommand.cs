using Nexo.Core.Application.Interfaces;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline.Steps;

public sealed class WriteGeneratedCommandCommand : ICommand<SelfExtendContext, SelfExtendContext>
{
    private readonly SelfExtendToolRuntime _rt;
    private readonly string? _messageOverride;

    public WriteGeneratedCommandCommand(SelfExtendToolRuntime rt, string? messageOverride = null)
    {
        _rt = rt;
        _messageOverride = messageOverride;
    }

    public async ValueTask<SelfExtendContext> ExecuteAsync(SelfExtendContext input, CancellationToken ct)
    {
        var message = string.IsNullOrWhiteSpace(_messageOverride) ? input.Spec.Message : _messageOverride!;
        var content = SelfExtendTemplates.RenderGeneratedCommand(input, message);
        var result = await _rt.InvokeAsync(input, "repo.fs.write", new
        {
            root = input.RepoRoot,
            path = input.GeneratedCommandPath,
            content
        }, ct);

        input.History.Add(new { step = "write_command", file = input.GeneratedCommandPath, delta = result.Delta.Log, message });
        return input;
    }
}

