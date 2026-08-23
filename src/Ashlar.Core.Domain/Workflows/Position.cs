namespace Ashlar.Core.Domain.Workflows;

/// <summary>Canvas coordinates for a workflow node in the visual composer.</summary>
/// <param name="X">Horizontal position in composer units.</param>
/// <param name="Y">Vertical position in composer units.</param>
public record Position(double X, double Y);
