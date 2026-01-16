using Nexo.Core.Application.Interfaces;
using Nexo.Infrastructure.Execution;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline.Steps;

public sealed class LlmWriteGeneratedCommandCommand : ICommand<SelfExtendContext, SelfExtendContext>
{
    private readonly SelfExtendToolRuntime _rt;
    private readonly IProviderFactory _providerFactory;
    private readonly string _provider;
    private readonly string? _messageOverride;

    public LlmWriteGeneratedCommandCommand(
        SelfExtendToolRuntime rt,
        IProviderFactory providerFactory,
        string provider,
        string? messageOverride)
    {
        _rt = rt;
        _providerFactory = providerFactory;
        _provider = provider;
        _messageOverride = messageOverride;
    }

    public async ValueTask<SelfExtendContext> ExecuteAsync(SelfExtendContext input, CancellationToken ct)
    {
        if (!_providerFactory.IsProviderAvailable(_provider))
        {
            throw new InvalidOperationException($"LLM provider '{_provider}' is not available");
        }

        var message = string.IsNullOrWhiteSpace(_messageOverride) ? input.Spec.Message : _messageOverride!;

        var system = """
You generate a SINGLE C# source file.
Return ONLY raw C# (no markdown fences, no prose).
The output must compile as-is.
""";

        var user =
            "Create a System.CommandLine Command subclass for Nexo.\n\n" +
            "Requirements:\n" +
            "- namespace: Nexo.CLI.Commands.DemoGenerated\n" +
            $"- class name: {input.GeneratedCommandClassName}\n" +
            $"- command name: {input.Spec.CommandName}\n" +
            "- when invoked, write a single line JSON object to stdout:\n" +
            $"  {{ \"ok\": true, \"command\": \"{EscapeForPrompt(input.Spec.CommandName)}\", \"message\": \"{EscapeForPrompt(message)}\" }}\n" +
            "- exit code 0\n";

        var raw = await _providerFactory.ExecuteLLMAsync(_provider, system, user, config: new { }, cancellationToken: ct);
        var code = SanitiseSource(raw);

        // Basic validation: must include expected namespace + class.
        if (!code.Contains("namespace Nexo.CLI.Commands.DemoGenerated", StringComparison.Ordinal) ||
            !code.Contains($"class {input.GeneratedCommandClassName}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("LLM output did not look like the expected command source file");
        }

        var result = await _rt.InvokeAsync(input, "repo.fs.write", new
        {
            root = input.RepoRoot,
            path = input.GeneratedCommandPath,
            content = code
        }, ct);

        input.History.Add(new { step = "llm_write_command", provider = _provider, file = input.GeneratedCommandPath, delta = result.Delta.Log });
        return input;
    }

    private static string SanitiseSource(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var start = text.IndexOf("```", StringComparison.Ordinal);
        if (start >= 0)
        {
            var next = text.IndexOf('\n', start);
            if (next >= 0)
            {
                var end = text.IndexOf("```", next, StringComparison.Ordinal);
                if (end > next)
                {
                    return text.Substring(next + 1, end - (next + 1)).Trim();
                }
            }
        }

        return text.Trim();
    }

    private static string EscapeForPrompt(string s)
        => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
}

