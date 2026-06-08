namespace GameDirector.Domain;

public sealed record SessionStateSnapshot(string SessionId, string? AestheticSummary);

public interface IForgeSessionReader
{
    SessionStateSnapshot? GetSession(string sessionId);
}

public sealed class ForgeStateSessionReader : IForgeSessionReader
{
    private readonly Func<string> _getSessionId;
    private readonly Func<string?> _getAestheticSummary;

    public ForgeStateSessionReader(Func<string> getSessionId, Func<string?> getAestheticSummary)
    {
        _getSessionId = getSessionId;
        _getAestheticSummary = getAestheticSummary;
    }

    public SessionStateSnapshot? GetSession(string sessionId)
    {
        var current = _getSessionId();
        if (!string.IsNullOrEmpty(sessionId) &&
            !string.Equals(current, sessionId, StringComparison.OrdinalIgnoreCase))
            return new SessionStateSnapshot(sessionId, _getAestheticSummary());

        return new SessionStateSnapshot(current, _getAestheticSummary());
    }
}
