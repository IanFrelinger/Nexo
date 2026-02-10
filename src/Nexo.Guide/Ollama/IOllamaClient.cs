namespace Nexo.Guide.Ollama;

public interface IOllamaClient
{
    Task<string> GenerateAsync(IReadOnlyList<OllamaMessage> messages, CancellationToken cancellationToken = default);
}

public record OllamaMessage(string Role, string Content);
