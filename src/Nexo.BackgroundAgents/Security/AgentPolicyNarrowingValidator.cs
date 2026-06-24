using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.BackgroundAgents.Registry;

namespace Nexo.BackgroundAgents.Security;

/// <summary>
/// Enforces child ⊆ creator exfiltration/sensitivity envelope at machine-spawn registration.
/// Human-authored roots (no <see cref="BackgroundAgentConfig.ParentId"/>) are trust roots and skip validation.
/// </summary>
public static class AgentPolicyNarrowingValidator
{
    public static void ValidateOrThrow(
        BackgroundAgentConfig child,
        Func<string, BackgroundAgentInstance?> resolveParent,
        IDataSensitivityRegistry sensitivityRegistry)
    {
        if (string.IsNullOrWhiteSpace(child.ParentId))
            return;

        if (sensitivityRegistry is null)
        {
            throw new InvalidOperationException(
                $"Refusing machine-spawned agent '{child.Id}': sensitivity registry unavailable to verify parent envelope.");
        }

        var parentInstance = resolveParent(child.ParentId);
        if (parentInstance is null)
        {
            throw new InvalidOperationException(
                $"Refusing machine-spawned agent '{child.Id}': parent '{child.ParentId}' is not registered.");
        }

        var parent = parentInstance.Config;
        var parentPolicy = parent.ExfiltrationPolicy ?? new ExfiltrationPolicy();
        var childPolicy = child.ExfiltrationPolicy ?? new ExfiltrationPolicy();

        if (parentPolicy.BlockExternalLLMs && !childPolicy.BlockExternalLLMs)
        {
            throw new InvalidOperationException(
                $"Refusing machine-spawned agent '{child.Id}': BlockExternalLLMs cannot be relaxed below parent '{parent.Id}'.");
        }

        if (parentPolicy.BlockWebSearch && !childPolicy.BlockWebSearch)
        {
            throw new InvalidOperationException(
                $"Refusing machine-spawned agent '{child.Id}': BlockWebSearch cannot be relaxed below parent '{parent.Id}'.");
        }

        if (parentPolicy.BlockNetworkExports && !childPolicy.BlockNetworkExports)
        {
            throw new InvalidOperationException(
                $"Refusing machine-spawned agent '{child.Id}': BlockNetworkExports cannot be relaxed below parent '{parent.Id}'.");
        }

        if (parentPolicy.RequireLocalOnly && !childPolicy.RequireLocalOnly)
        {
            throw new InvalidOperationException(
                $"Refusing machine-spawned agent '{child.Id}': RequireLocalOnly cannot be relaxed below parent '{parent.Id}'.");
        }

        EnsureLevelNotBroader(
            child.Id,
            parent.Id,
            "MaxAllowedLevel",
            parentPolicy.MaxAllowedLevel ?? parent.MaxDataSensitivity,
            childPolicy.MaxAllowedLevel ?? child.MaxDataSensitivity,
            sensitivityRegistry);

        EnsureLevelNotBroader(
            child.Id,
            parent.Id,
            "MaxDataSensitivity",
            parent.MaxDataSensitivity,
            child.MaxDataSensitivity,
            sensitivityRegistry);

        if (child.AllowedDataSensitivityLevels is { Count: > 0 })
        {
            var parentAllowed = parent.AllowedDataSensitivityLevels is { Count: > 0 }
                ? new HashSet<string>(parent.AllowedDataSensitivityLevels, StringComparer.OrdinalIgnoreCase)
                : null;

            foreach (var levelName in child.AllowedDataSensitivityLevels)
            {
                if (parentAllowed is not null && !parentAllowed.Contains(levelName))
                {
                    throw new InvalidOperationException(
                        $"Refusing machine-spawned agent '{child.Id}': AllowedDataSensitivityLevels contains '{levelName}' outside parent '{parent.Id}' allow-list.");
                }

                EnsureLevelNotBroader(
                    child.Id,
                    parent.Id,
                    "AllowedDataSensitivityLevels",
                    parentPolicy.MaxAllowedLevel ?? parent.MaxDataSensitivity,
                    levelName,
                    sensitivityRegistry);
            }
        }
    }

    private static void EnsureLevelNotBroader(
        string childId,
        string parentId,
        string field,
        string parentLevelName,
        string childLevelName,
        IDataSensitivityRegistry sensitivityRegistry)
    {
        var parentLevel = sensitivityRegistry.GetByName(parentLevelName);
        var childLevel = sensitivityRegistry.GetByName(childLevelName);
        if (parentLevel is null || childLevel is null)
        {
            throw new InvalidOperationException(
                $"Refusing machine-spawned agent '{childId}': unable to resolve {field} sensitivity levels for parent/child comparison.");
        }

        if (childLevel.SensitivityValue > parentLevel.SensitivityValue)
        {
            throw new InvalidOperationException(
                $"Refusing machine-spawned agent '{childId}': {field} '{childLevelName}' exceeds parent '{parentId}' envelope '{parentLevelName}'.");
        }
    }
}
