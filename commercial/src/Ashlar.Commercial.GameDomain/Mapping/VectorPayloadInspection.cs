namespace Ashlar.Commercial.GameDomain.Mapping;

/// <param name="FormatGuess">Heuristic format category.</param>
/// <param name="Snippet">Optional short UTF-8 preview or hex prefix.</param>
/// <param name="Notes">Diagnostic notes for operators.</param>
public sealed record VectorPayloadInspection(string FormatGuess, string Snippet, IReadOnlyList<string> Notes);
