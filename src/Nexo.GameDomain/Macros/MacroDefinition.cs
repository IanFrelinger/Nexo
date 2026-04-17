namespace Nexo.GameDomain.Macros;

/// <summary>
/// Defines a reusable macro — a named sequence of steps that automate repetitive game
/// design operations such as spawning preset weapon loadouts, configuring AI waves,
/// or applying aesthetic themes.
/// <para>
/// Macros are versioned, authored artefacts that can be shared, exported, and imported
/// across sessions via the <see cref="MacroRegistry"/>.
/// </para>
/// </summary>
public sealed record MacroDefinition
{
    /// <summary>Unique macro identifier.</summary>
    public string MacroId { get; init; } = string.Empty;

    /// <summary>Human-readable name shown in the macro palette.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Free-text description of what the macro does.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Identifier of the user who created this macro.</summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>Semantic version string (e.g. <c>"1.0.0"</c>).</summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>Free-form tags for filtering and discovery.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Ordered list of user-configurable parameters exposed by this macro.</summary>
    public IReadOnlyList<MacroParameter> Parameters { get; init; } = [];

    /// <summary>Ordered sequence of steps executed when the macro runs.</summary>
    public IReadOnlyList<MacroStep> Steps { get; init; } = [];

    /// <summary>
    /// Identifiers of other macros that must be registered before this macro can execute.
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; init; } = [];

    /// <summary>
    /// Trust level required to execute this macro (e.g. <c>"community"</c>, <c>"verified"</c>,
    /// <c>"system"</c>).
    /// </summary>
    public string TrustLevel { get; init; } = "community";
}

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

/// <summary>
/// A single action within a <see cref="MacroDefinition"/>'s execution sequence.
/// </summary>
public sealed record MacroStep
{
    /// <summary>
    /// Action verb identifying the operation to perform (e.g. <c>"spawn_weapon"</c>,
    /// <c>"set_rule"</c>, <c>"apply_aesthetic"</c>).
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>Key-value arguments passed to the action handler.</summary>
    public IReadOnlyDictionary<string, object> Args { get; init; } =
        new Dictionary<string, object>();
}
