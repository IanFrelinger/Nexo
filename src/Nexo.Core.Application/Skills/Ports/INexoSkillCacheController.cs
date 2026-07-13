namespace Nexo.Core.Application.Skills.Ports;

/// <summary>
/// Runtime-controlled skill cache invalidation (content-hash based).
/// </summary>
public interface INexoSkillCacheController
{
    void InvalidateAll();

    void InvalidateIfContentChanged(string skillsRootPath);

    string ComputeContentHash(string skillsRootPath);
}
