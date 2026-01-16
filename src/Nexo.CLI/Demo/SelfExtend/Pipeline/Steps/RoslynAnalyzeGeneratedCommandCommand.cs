using System.Text.Json;
using Nexo.Core.Application.Interfaces;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline.Steps;

/// <summary>
/// Runs Roslyn-based consistency checks on the generated command source file.
/// Fails the pipeline if any violations are found.
/// </summary>
public sealed class RoslynAnalyzeGeneratedCommandCommand : ICommand<SelfExtendContext, SelfExtendContext>
{
    private readonly SelfExtendToolRuntime _rt;

    public RoslynAnalyzeGeneratedCommandCommand(SelfExtendToolRuntime rt) => _rt = rt;

    public async ValueTask<SelfExtendContext> ExecuteAsync(SelfExtendContext input, CancellationToken ct)
    {
        var args = new
        {
            root = input.RepoRoot,
            files = new[] { input.GeneratedCommandPath },
            rules = new
            {
                requiredNamespace = "Nexo.CLI.Commands.DemoGenerated",
                requireFileScopedNamespace = true,
                requiredBaseType = "Command",
                requirePublic = true,
                requireSealed = true,
                requiredClassName = input.GeneratedCommandClassName,
                requiredCommandName = input.Spec.CommandName
            }
        };

        var result = await _rt.InvokeAsync(input, "roslyn.analyze", args, ct);
        var payloadJson = JsonSerializer.Serialize(result.Payload);
        using var doc = JsonDocument.Parse(payloadJson);
        var ok = doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();

        input.History.Add(new { step = "roslyn_analyze", ok, delta = result.Delta.Log });

        if (!ok)
        {
            input.LastTestOk = false;
            var violations = doc.RootElement.TryGetProperty("violations", out var v) ? v.ToString() : "<none>";
            throw new InvalidOperationException($"Roslyn consistency checks failed: {violations}");
        }

        return input;
    }
}

