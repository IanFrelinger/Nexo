using System.Threading;
using Nexo.Abstractions.Barriers;

namespace Nexo.Runtime.Barriers;

/// <summary>
/// AsyncLocal-backed ambient barrier context for singleton consumers.
/// </summary>
public sealed class BarrierContextAmbient : IBarrierContextAmbient
{
    private static readonly AsyncLocal<BarrierContext?> Holder = new();

    public BarrierContext? Current => Holder.Value;

    public void SetCurrent(BarrierContext? context) => Holder.Value = context;
}
