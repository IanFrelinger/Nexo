namespace Ashlar.Core.Application.Certification.Models;

/// <summary>Typed input or output port on a composition boundary or node.</summary>
/// <param name="Name">Port name used in wiring and witness cases.</param>
/// <param name="Type">Logical type label for compatibility checks.</param>
public sealed record CompositionPort(string Name, string Type);
