namespace Nexo.Commercial.GameDomain.Aesthetics;

/// <summary>
/// Validates <see cref="AestheticPack"/> adaptation fields for typos and structural mistakes.
/// </summary>
public static class AestheticPackValidation
{
    /// <summary>
    /// Validates <paramref name="pack"/>; returns issues. Empty list means no findings.
    /// </summary>
    public static IReadOnlyList<AestheticValidationIssue> Validate(
        AestheticPack pack,
        AestheticPackValidationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(pack);
        options ??= new AestheticPackValidationOptions();

        var issues = new List<AestheticValidationIssue>();

        if (string.IsNullOrWhiteSpace(pack.Id))
            issues.Add(new("aesthetic.empty_id", "AestheticPack.Id is required."));

        if (options.RequireKnownGeometryStrategy &&
            !AestheticAdaptationCatalog.IsKnownGeometryStrategy(pack.GeometryStrategy))
        {
            issues.Add(new(
                "aesthetic.unknown_geometry_strategy",
                $"GeometryStrategy '{pack.GeometryStrategy}' is not a known value. Expected one of: {string.Join(", ", GeometryStrategies.All)}."));
        }

        if (options.RequireKnownRenderingPipelineKind &&
            !AestheticAdaptationCatalog.IsKnownRenderingPipelineKind(pack.RenderingPipelineKind))
        {
            issues.Add(new(
                "aesthetic.unknown_rendering_pipeline_kind",
                $"RenderingPipelineKind '{pack.RenderingPipelineKind}' is not known. Expected one of: {string.Join(", ", AestheticAdaptationCatalog.KnownRenderingPipelineKinds)}."));
        }

        var seenBindings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var b in pack.EngineSurfaceBindings)
        {
            if (string.IsNullOrWhiteSpace(b.EngineId))
            {
                issues.Add(new("binding.empty_engine_id", "EngineSurfaceBinding.EngineId must not be empty."));
                continue;
            }

            if (options.RequireKnownEngineIds && !AestheticAdaptationCatalog.IsKnownEngineId(b.EngineId))
            {
                issues.Add(new(
                    "binding.unknown_engine_id",
                    $"EngineId '{b.EngineId}' is not in the known engine catalog. Use GameEngines constants or set RequireKnownEngineIds=false."));
            }

            if (string.IsNullOrWhiteSpace(b.Role))
                issues.Add(new("binding.empty_role", "EngineSurfaceBinding.Role must not be empty."));

            if (string.IsNullOrWhiteSpace(b.MaterialSurfaceId))
                issues.Add(new("binding.empty_material_surface", "EngineSurfaceBinding.MaterialSurfaceId must not be empty."));

            if (options.WarnOnUndocumentedRolesAndSurfaces &&
                !string.IsNullOrWhiteSpace(b.Role) &&
                !RenderingSurfaceRoles.Documented.Contains(b.Role.Trim()))
            {
                issues.Add(new(
                    "binding.undocumented_role",
                    $"Role '{b.Role}' is not in the documented interoperability set; hosts may still support it."));
            }

            if (options.WarnOnUndocumentedRolesAndSurfaces &&
                !string.IsNullOrWhiteSpace(b.MaterialSurfaceId) &&
                !MaterialSurfaceIds.Documented.Contains(b.MaterialSurfaceId.Trim()))
            {
                issues.Add(new(
                    "binding.undocumented_material_surface",
                    $"MaterialSurfaceId '{b.MaterialSurfaceId}' is not in the documented set; hosts may still map it."));
            }

            var key = $"{b.EngineId.Trim()}\0{b.Role.Trim()}";
            if (!string.IsNullOrWhiteSpace(b.Role) && !seenBindings.Add(key))
            {
                issues.Add(new(
                    "binding.duplicate_engine_role",
                    $"Duplicate EngineSurfaceBinding for engine '{b.EngineId}' and role '{b.Role}'."));
            }
        }

        return issues;
    }

    /// <summary>
    /// Returns true when there are no blocking issues (errors). Undocumented role/surface codes are treated as non-blocking when <paramref name="treatUndocumentedAsNonBlocking"/> is true.
    /// </summary>
    public static bool IsValid(
        IReadOnlyList<AestheticValidationIssue> issues,
        bool treatUndocumentedAsNonBlocking = true)
    {
        foreach (var i in issues)
        {
            if (treatUndocumentedAsNonBlocking && i.Code.StartsWith("binding.undocumented_", StringComparison.Ordinal))
                continue;

            return false;
        }

        return true;
    }
}
