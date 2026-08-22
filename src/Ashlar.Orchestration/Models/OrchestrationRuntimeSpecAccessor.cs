using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;

namespace Ashlar.Orchestration.Models;

/// <summary>AsyncLocal-backed implementation of <see cref="IOrchestrationRuntimeSpecAccessor"/>.</summary>
public sealed class OrchestrationRuntimeSpecAccessor : IOrchestrationRuntimeSpecAccessor
{
    private static readonly AsyncLocal<OrchestrationRuntimeSpec?> Ambient = new();

    /// <inheritdoc />
    public OrchestrationRuntimeSpec Current => Ambient.Value ?? OrchestrationRuntimeSpec.Default();

    /// <inheritdoc />
    public IDisposable Begin(OrchestrationRuntimeSpec spec)
    {
        var prior = Ambient.Value;
        Ambient.Value = spec;
        return new Scope(() => Ambient.Value = prior);
    }

    private sealed class Scope : IDisposable
    {
        private readonly Action _dispose;
        private bool _done;
        public Scope(Action dispose) => _dispose = dispose;
        public void Dispose()
        {
            if (_done) return;
            _done = true;
            _dispose();
        }
    }
}
