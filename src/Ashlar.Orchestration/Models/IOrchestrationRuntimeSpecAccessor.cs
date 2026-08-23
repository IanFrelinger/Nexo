using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;

namespace Ashlar.Orchestration.Models;

/// <summary>Ambient accessor for the active orchestration runtime specification.</summary>
public interface IOrchestrationRuntimeSpecAccessor
{
    /// <summary>Currently active runtime spec, or the default when none is scoped.</summary>
    OrchestrationRuntimeSpec Current { get; }

    /// <summary>Scopes <paramref name="spec"/> for the current async context until the returned handle is disposed.</summary>
    IDisposable Begin(OrchestrationRuntimeSpec spec);
}
