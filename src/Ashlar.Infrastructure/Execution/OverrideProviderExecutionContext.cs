using Ashlar.Core.Domain.Execution;

namespace Ashlar.Infrastructure.Execution;

/// <summary>
/// Wraps an execution context with an overridden provider (for per-brick config).
/// </summary>
public sealed class OverrideProviderExecutionContext : IExecutionContext
{
    private readonly IExecutionContext _inner;
    private readonly string _provider;

    /// <summary>Wraps an execution context with an overridden provider for per-brick configuration.</summary>
    public OverrideProviderExecutionContext(IExecutionContext inner, string provider)
    {
        _inner = inner;
        _provider = provider;
    }

    /// <inheritdoc />
    public string AgentId => _inner.AgentId;
    /// <inheritdoc />
    public string BehaviorId => _inner.BehaviorId;
    /// <inheritdoc />
    public bool IsAirGapped => _inner.IsAirGapped;
    /// <inheritdoc />
    public bool AuditMode => _inner.AuditMode;
    /// <summary>The overridden provider (e.g. ollama, video) for this brick.</summary>
    public string Provider => _provider;
    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Variables => _inner.Variables;
}
