using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Ashlar.AI.Pipeline.Clients;

/// <summary>
/// Test spy that records invocations and returns a fixed response.
/// </summary>
public sealed class SpyChatClient : IChatClient
{
    private readonly string _response;
    private readonly ConcurrentQueue<IReadOnlyList<ChatMessage>> _calls = new();

    /// <summary>Creates a spy client.</summary>
    public SpyChatClient(string response = "spy-ok")
    {
        _response = response;
    }

    /// <summary>Captured outbound message lists (one per call).</summary>
    public IReadOnlyList<IReadOnlyList<ChatMessage>> Calls => _calls.ToArray();

    /// <summary>Number of times the spy was invoked.</summary>
    public int CallCount => _calls.Count;

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _calls.Enqueue(messages.ToList());
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _response))
        {
            ModelId = options?.ModelId ?? "spy",
        });
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _calls.Enqueue(messages.ToList());
        await Task.Yield();
        yield return new ChatResponseUpdate(ChatRole.Assistant, _response)
        {
            ModelId = options?.ModelId ?? "spy",
        };
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType.IsInstanceOfType(this) ? this : null;

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
