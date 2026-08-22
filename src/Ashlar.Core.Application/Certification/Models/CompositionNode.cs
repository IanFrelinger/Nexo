namespace Ashlar.Core.Application.Certification.Models;

/// <summary>A brick node within a composition graph.</summary>
/// <param name="NodeId">Stable node identifier within the composition.</param>
/// <param name="BrickId">Referenced certified brick id.</param>
public sealed record CompositionNode(string NodeId, string BrickId);
