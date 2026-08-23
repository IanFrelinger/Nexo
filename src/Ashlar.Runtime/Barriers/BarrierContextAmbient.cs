using System.Threading;
using Ashlar.Abstractions.Barriers;

namespace Ashlar.Runtime.Barriers;

/// <summary>
/// AsyncLocal-backed ambient barrier context for singleton consumers.
/// </summary>
public sealed class BarrierContextAmbient : IBarrierContextAmbient
{
    private static readonly AsyncLocal<BarrierContext?> Holder = new();

    /// <summary>Barrier context for the current async flow, if any.</summary>
    public BarrierContext? Current => Holder.Value;

    /// <summary>Sets the ambient barrier context for the current async flow.</summary>
    public void SetCurrent(BarrierContext? context) => Holder.Value = context;
}
