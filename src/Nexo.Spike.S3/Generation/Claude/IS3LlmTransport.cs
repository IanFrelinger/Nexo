namespace Nexo.Spike.S3.Generation.Claude;

public sealed record S3LlmCompletionRequest(
    string Model,
    string SystemPrompt,
    string UserPrompt,
    double Temperature);

public interface IS3LlmTransport
{
    Task<string> CompleteAsync(S3LlmCompletionRequest request, CancellationToken ct = default);
}
