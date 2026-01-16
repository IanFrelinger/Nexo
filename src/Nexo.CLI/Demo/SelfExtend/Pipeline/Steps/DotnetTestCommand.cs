using System.Text.Json;
using Nexo.Core.Application.Interfaces;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline.Steps;

public sealed class DotnetTestCommand : ICommand<SelfExtendContext, SelfExtendContext>
{
    private readonly SelfExtendToolRuntime _rt;

    public DotnetTestCommand(SelfExtendToolRuntime rt) => _rt = rt;

    public async ValueTask<SelfExtendContext> ExecuteAsync(SelfExtendContext input, CancellationToken ct)
    {
        var result = await _rt.InvokeAsync(input, "dotnet.test", new { root = input.RepoRoot }, ct);

        var payloadJson = JsonSerializer.Serialize(result.Payload);
        using var doc = JsonDocument.Parse(payloadJson);
        var ok = doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();
        var stdout = doc.RootElement.TryGetProperty("stdout", out var soProp) ? soProp.GetString() : null;
        var stderr = doc.RootElement.TryGetProperty("stderr", out var seProp) ? seProp.GetString() : null;

        input.LastTestOk = ok;
        input.LastTestStdout = stdout;
        input.LastTestStderr = stderr;

        input.History.Add(new { step = "dotnet_test", ok, delta = result.Delta.Log });
        return input;
    }
}

