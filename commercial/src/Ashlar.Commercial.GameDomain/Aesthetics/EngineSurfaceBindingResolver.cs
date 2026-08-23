namespace Ashlar.Commercial.GameDomain.Aesthetics;
/// <summary>
/// Helpers for hosts resolving <see cref="EngineRenderingSurfaceBinding"/> entries on an <see cref="AestheticPack"/>.
/// </summary>
public static class EngineSurfaceBindingResolver
{
    /// <summary>
    /// Returns the first binding matching <paramref name="engineId"/> and <paramref name="role"/>,
    /// using ordinal case-insensitive comparison for ids.
    /// </summary>
    public static EngineRenderingSurfaceBinding? TryGetBinding(
        AestheticPack pack,
        string engineId,
        string role)
    {
        ArgumentNullException.ThrowIfNull(pack);

        if (string.IsNullOrWhiteSpace(engineId) || string.IsNullOrWhiteSpace(role))
            return null;

        foreach (var b in pack.EngineSurfaceBindings)
        {
            if (string.Equals(b.EngineId, engineId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(b.Role, role, StringComparison.OrdinalIgnoreCase))
                return b;
        }

        return null;
    }
}
