using System.Text.Json;
using Nexo.Core.Application.Interfaces;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline.Steps;

/// <summary>
/// Runs the framework's internal test runner via the CLI (`nexo test`) so the demo uses
/// the same aggregator/test-discovery mechanism as the rest of the system.
/// </summary>
public sealed class RunNexoTestCommand : ICommand<SelfExtendContext, SelfExtendContext>
{
    private readonly SelfExtendToolRuntime _rt;
    private readonly string _filter;

    public RunNexoTestCommand(SelfExtendToolRuntime rt, string filter)
    {
        _rt = rt;
        _filter = filter;
    }

    public async ValueTask<SelfExtendContext> ExecuteAsync(SelfExtendContext input, CancellationToken ct)
    {
        var result = await _rt.InvokeAsync(input, "dotnet.run", new
        {
            root = input.RepoRoot,
            args = $"run --project src/Nexo.CLI -- test --format-json --filter \"{_filter}\"",
            timeoutSeconds = 1200
        }, ct);

        var payloadJson = JsonSerializer.Serialize(result.Payload);
        using var payloadDoc = JsonDocument.Parse(payloadJson);
        var exitCode = payloadDoc.RootElement.TryGetProperty("exitCode", out var ec) ? ec.GetInt32() : -1;
        var stdout = payloadDoc.RootElement.TryGetProperty("stdout", out var so) ? so.GetString() ?? "" : "";
        var stderr = payloadDoc.RootElement.TryGetProperty("stderr", out var se) ? se.GetString() ?? "" : "";

        // CLI may print logs + JSON; extract the JSON object containing TotalTests.
        var json = ExtractJsonObject(stdout);
        using var doc = JsonDocument.Parse(json);
        var total = doc.RootElement.GetProperty("TotalTests").GetInt32();
        var failed = doc.RootElement.GetProperty("FailedTests").GetInt32();

        input.LastTestOk = exitCode == 0 && failed == 0 && total > 0;
        input.LastTestStdout = stdout;
        input.LastTestStderr = stderr;

        input.History.Add(new
        {
            step = "nexo_test",
            filter = _filter,
            exitCode,
            total,
            failed
        });

        return input;
    }

    private static string ExtractJsonObject(string text)
    {
        // Take from first '{' to last '}'.
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end < 0 || end <= start)
        {
            throw new InvalidOperationException("No JSON object found in output");
        }
        return text.Substring(start, end - start + 1);
    }
}

