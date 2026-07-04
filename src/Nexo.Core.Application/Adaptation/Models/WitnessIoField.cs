namespace Nexo.Core.Application.Adaptation.Models;

/// <summary>
/// Named I/O field in a witness signature (name and type only — no expected values).
/// </summary>
/// <param name="Name">Port or parameter name.</param>
/// <param name="Type">Inferred or declared type label (e.g. string, int).</param>
public sealed record WitnessIoField(string Name, string Type);
