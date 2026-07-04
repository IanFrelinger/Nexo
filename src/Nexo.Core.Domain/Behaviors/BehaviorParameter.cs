using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Domain.Behaviors;

/// <summary>
/// Input or output parameter for a behavior.
/// </summary>
public class BehaviorParameter
{
    /// <summary>Parameter name referenced in behavior input/output mappings.</summary>
    public string Name { get; init; } = default!;

    /// <summary>Logical type name (e.g. <c>string</c>, <c>number</c>).</summary>
    public string Type { get; init; } = default!;

    /// <summary>Whether callers must supply this parameter.</summary>
    public bool Required { get; init; } = true;

    /// <summary>Default value when <see cref="Required"/> is false.</summary>
    public object? Default { get; init; }

    /// <summary>Optional description shown in composer UIs.</summary>
    public string? Description { get; init; }

    /// <summary>Defines a behavior input or output parameter.</summary>
    public BehaviorParameter(string name, string type, bool required = true, object? defaultValue = null, string? description = null)
    {
        Name = name;
        Type = type;
        Required = required;
        Default = defaultValue;
        Description = description;
    }
}
