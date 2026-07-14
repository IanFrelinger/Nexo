using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Nexo.AI.Pipeline.Clients;

/// <summary>
/// Test / offline double that returns a fixed assistant reply.
/// </summary>
public sealed class FakeChatClient : IChatClient
{
    private readonly string _response;
    private readonly string _modelId;

    /// <summary>Creates a fake client that always returns <paramref name="response"/>.</summary>
    public FakeChatClient(string response = "fake-response", string modelId = "fake")
    {
        _response = response;
        _modelId = modelId;
    }

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _response))
        {
            ModelId = options?.ModelId ?? _modelId,
        });
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return new ChatResponseUpdate(ChatRole.Assistant, _response)
        {
            ModelId = options?.ModelId ?? _modelId,
        };
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(ChatClientMetadata))
        {
            return new ChatClientMetadata("fake", providerUri: null, defaultModelId: _modelId);
        }

        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
