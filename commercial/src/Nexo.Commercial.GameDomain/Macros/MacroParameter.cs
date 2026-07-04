namespace Nexo.Commercial.GameDomain.Macros;

/// <summary>
/// A single user-configurable parameter exposed by a <see cref="MacroDefinition"/>.
/// </summary>
public sealed record MacroParameter
{
    /// <summary>Parameter name used as a key when supplying values at invocation time.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Data type hint (e.g. <c>"int"</c>, <c>"double"</c>, <c>"string"</c>, <c>"bool"</c>).
    /// </summary>
    public string Type { get; init; } = "string";

    /// <summary>Default value used when the caller does not supply an explicit override.</summary>
    public object? DefaultValue { get; init; }

    /// <summary>Optional inclusive lower bound for numeric parameters.</summary>
    public double? Min { get; init; }

    /// <summary>Optional inclusive upper bound for numeric parameters.</summary>
    public double? Max { get; init; }
}
