namespace Ashlar.Commercial.GameDomain.Scoping;
/// <summary>
/// Resolves the effective value of scoped settings by walking the scope hierarchy
/// from narrowest to broadest.
/// <para>
/// <b>Precedence chain</b> (first match wins):
/// <c>Moment → Player → Object → Zone → Team → Server</c>.
/// </para>
/// <para>
/// At each tier the resolver checks whether the <see cref="ScopeContext"/> supplies a
/// matching target identifier. Expired settings (past <see cref="ScopedSetting.ExpiresAt"/>)
/// are silently skipped.
/// </para>
/// </summary>
public class ScopeResolver
{
    private readonly IReadOnlyList<ScopedSetting> _settings;

    /// <summary>
    /// Initialises the resolver with the full set of scoped settings for the current session.
    /// </summary>
    /// <param name="settings">All active scoped settings, in any order.</param>
    public ScopeResolver(IReadOnlyList<ScopedSetting> settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Resolves the effective value for a single setting within the given context.
    /// </summary>
    /// <param name="settingId">The setting identifier to look up.</param>
    /// <param name="context">Contextual identifiers describing the query point.</param>
    /// <returns>The value from the narrowest matching scope, or <c>null</c> if no match is found.</returns>
    public object? Resolve(string settingId, ScopeContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var candidates = GetActiveCandidates(settingId, now);

        return WalkScopes(candidates, context);
    }

    /// <summary>
    /// Resolves all settings that have at least one active match in the given context.
    /// </summary>
    /// <param name="context">Contextual identifiers describing the query point.</param>
    /// <returns>A dictionary mapping each resolved setting id to its effective value.</returns>
    public Dictionary<string, object> ResolveAll(ScopeContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var result = new Dictionary<string, object>();

        var settingIds = new HashSet<string>();
        foreach (var s in _settings)
        {
            settingIds.Add(s.SettingId);
        }

        foreach (var id in settingIds)
        {
            var candidates = GetActiveCandidates(id, now);
            var value = WalkScopes(candidates, context);
            if (value is not null)
            {
                result[id] = value;
            }
        }

        return result;
    }

    private List<ScopedSetting> GetActiveCandidates(string settingId, DateTimeOffset now)
    {
        var candidates = new List<ScopedSetting>();
        foreach (var s in _settings)
        {
            if (s.SettingId == settingId && (s.ExpiresAt is null || s.ExpiresAt > now))
            {
                candidates.Add(s);
            }
        }
        return candidates;
    }

    private static object? WalkScopes(List<ScopedSetting> candidates, ScopeContext context)
    {
        if (TryFindMatch(candidates, SettingScopeType.Moment, context.ActiveMoment, out var value))
            return value;
        if (TryFindMatch(candidates, SettingScopeType.Player, context.PlayerId, out value))
            return value;
        if (TryFindMatch(candidates, SettingScopeType.Object, context.ObjectId, out value))
            return value;
        if (TryFindMatch(candidates, SettingScopeType.Zone, context.ZoneId, out value))
            return value;
        if (TryFindMatch(candidates, SettingScopeType.Team, context.TeamId, out value))
            return value;

        foreach (var s in candidates)
        {
            if (s.Scope.Type == SettingScopeType.Server)
                return s.Value;
        }

        return null;
    }

    private static bool TryFindMatch(
        List<ScopedSetting> candidates,
        SettingScopeType type,
        string? target,
        out object? value)
    {
        value = null;
        if (target is null) return false;

        foreach (var s in candidates)
        {
            if (s.Scope.Type == type && s.Scope.Target == target)
            {
                value = s.Value;
                return true;
            }
        }

        return false;
    }
}
