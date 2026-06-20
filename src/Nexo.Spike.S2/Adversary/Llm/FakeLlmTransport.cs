namespace Nexo.Spike.S2.Adversary.Llm;

/// <summary>
/// Offline scripted transport for tests — no network, no secrets.
/// </summary>
public sealed class FakeLlmTransport : ILlmTransport
{
    private readonly IReadOnlyList<string> _scriptedResponses;
    private int _callIndex;

    public FakeLlmTransport(IReadOnlyList<string> scriptedResponses)
    {
        if (scriptedResponses.Count == 0)
            throw new ArgumentException("At least one scripted response is required.", nameof(scriptedResponses));

        _scriptedResponses = scriptedResponses;
    }

    public int CallCount => _callIndex;

    public Task<string> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var index = Math.Min(_callIndex, _scriptedResponses.Count - 1);
        var response = _scriptedResponses[index];
        _callIndex++;
        return Task.FromResult(response);
    }

    internal static string WrapAsModelResponse(string implementationSource) =>
        $"```csharp\n{implementationSource}\n```";
}
