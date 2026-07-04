namespace Nexo.Commercial.GameDomain.Macros;

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
