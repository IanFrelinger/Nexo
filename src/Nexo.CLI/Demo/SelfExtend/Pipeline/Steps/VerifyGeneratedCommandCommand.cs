using System.Diagnostics;
using System.Text.Json;
using Nexo.Core.Application.Interfaces;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline.Steps;

/// <summary>
/// Ephemeral verification step (does not generate test source files).
/// Runs the generated command and checks its JSON contract against the spec.
/// </summary>
public sealed class VerifyGeneratedCommandCommand : ICommand<SelfExtendContext, SelfExtendContext>
{
    public async ValueTask<SelfExtendContext> ExecuteAsync(SelfExtendContext input, CancellationToken ct)
    {
        var (exitCode, stdout, stderr) = await RunDotnetAsync(
            input.RepoRoot,
            $"run --project src/Nexo.CLI -- demo {input.Spec.CommandName} --format-json",
            ct);

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

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunDotnetAsync(string workingDir, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = args,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process");
        var stdoutTask = p.StandardOutput.ReadToEndAsync();
        var stderrTask = p.StandardError.ReadToEndAsync();

        await p.WaitForExitAsync(ct);
        return (p.ExitCode, await stdoutTask, await stderrTask);
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

