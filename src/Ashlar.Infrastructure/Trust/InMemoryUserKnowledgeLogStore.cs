using System.Collections.Concurrent;
using Ashlar.Core.Application.Trust.Models;
using Ashlar.Core.Application.Trust.Ports;

namespace Ashlar.Infrastructure.Trust;

/// <summary>
/// In-memory user knowledge log. Useful for tests and when persistence is not needed.
/// </summary>
public sealed class InMemoryUserKnowledgeLogStore : IUserKnowledgeLogStore
{
    private readonly ConcurrentDictionary<string, UserKnowledgeLogEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task UpsertAsync(UserKnowledgeLogEntry entry, CancellationToken cancellationToken = default)
    {
        var existing = _entries.TryGetValue(entry.Id, out var current);
        var version = existing ? current!.Version + 1 : entry.Version;
        var createdAt = existing ? current!.CreatedAt : entry.CreatedAt;

        _entries[entry.Id] = entry with
        {
            Version = version,
            CreatedAt = createdAt,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (_entries.TryGetValue(id, out var e))
            _entries[id] = e with { DeletedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<UserKnowledgeLogEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (_entries.TryGetValue(id, out var e) && e.DeletedAt == null)
            return Task.FromResult<UserKnowledgeLogEntry?>(e);
        return Task.FromResult<UserKnowledgeLogEntry?>(null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<UserKnowledgeLogEntry>> GetAsync(string? dataType = null, int maxCount = 100, CancellationToken cancellationToken = default)
    {
        var query = _entries.Values.Where(e => e.DeletedAt == null);
        if (!string.IsNullOrEmpty(dataType))
            query = query.Where(e => string.Equals(e.DataType, dataType, StringComparison.OrdinalIgnoreCase));
        var list = query.OrderByDescending(e => e.UpdatedAt).Take(maxCount).ToList();
        return Task.FromResult<IReadOnlyList<UserKnowledgeLogEntry>>(list);
    }

    /// <inheritdoc />
    public async Task<string> ExportToJsonAsync(int maxCount = 1000, CancellationToken cancellationToken = default)
    {
        var entries = await GetAsync(null, maxCount, cancellationToken).ConfigureAwait(false);
        return UserKnowledgeLogExportHelper.ToJson(entries);
    }

    /// <inheritdoc />
    public async Task<string> ExportToMarkdownAsync(int maxCount = 1000, CancellationToken cancellationToken = default)
    {
        var entries = await GetAsync(null, maxCount, cancellationToken).ConfigureAwait(false);
        return UserKnowledgeLogExportHelper.ToMarkdown(entries);
    }
}
