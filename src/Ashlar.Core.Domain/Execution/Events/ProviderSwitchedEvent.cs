using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Domain.Execution.Events;

// Provider events
/// <summary>
/// Emitted when the LLM provider is switched at runtime.
/// </summary>
public class ProviderSwitchedEvent : ExecutionEvent
{
    /// <summary>Previous provider name.</summary>
    public string FromProvider { get; init; } = default!;
    /// <summary>New provider name.</summary>
    public string ToProvider { get; init; } = default!;
    
    public ProviderSwitchedEvent(string fromProvider, string toProvider)
        : base("provider_switched", DateTime.UtcNow)
    {
        FromProvider = fromProvider;
        ToProvider = toProvider;
    }
}
