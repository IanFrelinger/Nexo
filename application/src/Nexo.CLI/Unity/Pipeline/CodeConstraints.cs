namespace Nexo.CLI.Unity.Pipeline;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>C# coding standards enforced during Unity code generation.</summary>
public sealed record CodeConstraints
{
    /// <summary>Root namespace prefix applied to generated types.</summary>
    public string? NamespacePrefix { get; init; }

    /// <summary>Maximum lines allowed per generated source file.</summary>
    public int MaxFileLines { get; init; }

    /// <summary>When true, public contracts must expose interfaces.</summary>
    public bool RequireInterfaces { get; init; }

    /// <summary>When true, serialized fields must use <c>[SerializeField]</c>.</summary>
    public bool RequireSerializeField { get; init; } = true;

    /// <summary>Code patterns that generation must never emit.</summary>
    public IReadOnlyList<string> BannedPatterns { get; init; } = Array.Empty<string>();

    /// <summary><c>using</c> directives that every generated file must include.</summary>
    public IReadOnlyList<string> RequiredUsings { get; init; } = Array.Empty<string>();

    /// <summary>Test coverage expectation label (e.g. unit, integration).</summary>
    public string? TestCoverage { get; init; }
}
