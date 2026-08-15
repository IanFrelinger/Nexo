using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;

namespace Nexo.Spatial.Contracts;

/// <summary>
/// Scripted pose timeline for a single atom.
/// </summary>
public sealed record ScriptedAtomSequence
{
    /// <summary>Spatial atom identifier matching provider queries.</summary>
    public string AtomId { get; init; } = string.Empty;

    /// <summary>Ordered pose samples emitted by <see cref="FakeSpatialAnchorProvider.PublishNext"/>.</summary>
    public IReadOnlyList<PoseSample> Samples { get; init; } = Array.Empty<PoseSample>();
}
