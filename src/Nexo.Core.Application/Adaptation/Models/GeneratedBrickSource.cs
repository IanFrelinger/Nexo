namespace Nexo.Core.Application.Adaptation.Models;

/// <summary>
/// Untrusted model output with provenance marking.
/// </summary>
/// <param name="SourceCode">Generated C# implementation source.</param>
/// <param name="Provenance">Source identifier for the generator (model, fixture, etc.).</param>
/// <param name="ClassName">Declared class name in the generated source.</param>
/// <param name="Namespace">Declared namespace in the generated source.</param>
public sealed record GeneratedBrickSource(
    string SourceCode,
    string Provenance,
    string ClassName,
    string Namespace);
