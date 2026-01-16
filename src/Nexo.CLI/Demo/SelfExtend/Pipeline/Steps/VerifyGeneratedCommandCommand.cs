using System.Text.Json;
using Nexo.Core.Application.Interfaces;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline.Steps;

/// <summary>
/// Ephemeral verification step (does not generate test source files).
/// Runs the generated command and checks its JSON contract against the spec.
/// </summary>
public sealed class VerifyGeneratedCommandCommand : ICommand<SelfExtendContext, SelfExtendContext>
{
    private readonly SelfExtendToolRuntime _rt;

    public VerifyGeneratedCommandCommand(SelfExtendToolRuntime rt) => _rt = rt;

    public async ValueTask<SelfExtendContext> ExecuteAsync(SelfExtendContext input, CancellationToken ct)
    {
        var result = await _rt.InvokeAsync(input, "dotnet.run", new
        {
            root = input.RepoRoot,
            args = $"run --project src/Nexo.CLI -- demo {input.Spec.CommandName} --format-json",
            timeoutSeconds = 300
        }, ct);

        var payloadJson = JsonSerializer.Serialize(result.Payload);
        using var payloadDoc = JsonDocument.Parse(payloadJson);
        var exitCode = payloadDoc.RootElement.TryGetProperty("exitCode", out var ec) ? ec.GetInt32() : -1;
        var stdout = payloadDoc.RootElement.TryGetProperty("stdout", out var so) ? so.GetString() ?? "" : "";
        var stderr = payloadDoc.RootElement.TryGetProperty("stderr", out var se) ? se.GetString() ?? "" : "";

        input.LastTestStdout = stdout;
        input.LastTestStderr = stderr;

        var ok = exitCode == 0;
        string? message = null;

        if (ok)
        {
            try
            {
                var json = ExtractJsonObject(stdout);
                using var doc = JsonDocument.Parse(json);
                ok = doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();
                message = doc.RootElement.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : null;
                ok = ok && string.Equals(message, input.Spec.Message, StringComparison.Ordinal);
            }
            catch
            {
                ok = false;
            }
        }

        input.LastTestOk = ok;
        input.History.Add(new
        {
            step = "verify_generated_command",
            exitCode,
            ok,
            expectedMessage = input.Spec.Message,
            actualMessage = message
        });

        return input;
    }

    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end < 0 || end <= start)
        {
            throw new InvalidOperationException("No JSON object found in output");
        }
        return text.Substring(start, end - start + 1);
    }
}

