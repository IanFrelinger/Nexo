namespace Nexo.Core.Application.SelfContext.Ports;

/// <summary>
/// Generates changelog from promoted adaptation records (Phase F).
/// </summary>
public interface IChangelogGenerator
{
    /// <summary>
    /// Generates changelog markdown for promoted changes in the given time range.
    /// </summary>
    Task<string> GenerateAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default);
}
