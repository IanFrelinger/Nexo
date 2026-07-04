using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Bricks;

/// <summary>
/// Applies declared brick input defaults to materialized execution input.
/// </summary>
public static class BrickInputDefaults
{
    /// <summary>
    /// Populates missing inputs with their declared defaults without overwriting supplied values.
    /// </summary>
    public static void Apply(DomainBrick brick, BrickInput input)
    {
        var existing = input.ToDictionary();

        foreach (var definition in brick.Interface.Inputs)
        {
            if (definition.Default is null)
            {
                continue;
            }

            if (!existing.ContainsKey(definition.Name))
            {
                input.Set(definition.Name, definition.Default);
            }
        }
    }
}
