namespace GameDirector.Domain;

/// <summary>Contract for forge session reader.</summary>
public interface IForgeSessionReader
{
    /// <summary>Gets session.</summary>
    /// <param name="sessionId">Session id.</param>
    SessionStateSnapshot? GetSession(string sessionId);
}
