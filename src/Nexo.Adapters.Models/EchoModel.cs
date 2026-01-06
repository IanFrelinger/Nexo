using Nexo.Abstractions;

namespace Nexo.Adapters.Models;

/// <summary>
/// Echo/placeholder model implementation for testing (doesn't actually call LLM).
/// 
/// Provides:
/// - Mock implementation of IModel
/// - Returns the last message content as output
/// 
/// Used for testing and development when real LLM is not needed.
/// Implements IModel for use with agents that require LLM integration.
/// </summary>
public sealed class EchoModel : IModel
{
    public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct)
    {
        var text = input.Messages.LastOrDefault().content ?? "";
        return Task.FromResult(new ModelOutput(text));
    }
}
