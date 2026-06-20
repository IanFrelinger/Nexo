namespace Nexo.Spike.S2.Adversary.Llm;

public sealed record LlmCompletionRequest(
    string Model,
    string SystemPrompt,
    string UserPrompt,
    double? Temperature,
    int? Seed);

public interface ILlmTransport
{
    Task<string> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default);
}
