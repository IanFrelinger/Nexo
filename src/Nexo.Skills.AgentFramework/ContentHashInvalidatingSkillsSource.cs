using Microsoft.Agents.AI;
using Nexo.Core.Application.Skills.Ports;

namespace Nexo.Skills.AgentFramework;

/// <summary>
/// Invalidates cached skill lists when the content hash of skill roots changes.
/// </summary>
internal sealed class ContentHashInvalidatingSkillsSource : DelegatingAgentSkillsSource
{
    private readonly INexoSkillCacheController _cacheController;
    private readonly IReadOnlyList<string> _skillRootPaths;

    public ContentHashInvalidatingSkillsSource(
        AgentSkillsSource innerSource,
        INexoSkillCacheController cacheController,
        IEnumerable<string> skillRootPaths)
        : base(innerSource)
    {
        _cacheController = cacheController;
        _skillRootPaths = skillRootPaths.ToList();
    }

    public override Task<IList<AgentSkill>> GetSkillsAsync(
        AgentSkillsSourceContext context,
        CancellationToken cancellationToken = default)
    {
        foreach (var root in _skillRootPaths)
            _cacheController.InvalidateIfContentChanged(root);

        return InnerSource.GetSkillsAsync(context, cancellationToken);
    }
}
