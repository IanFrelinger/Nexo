using Nexo.Core.Application.Trust.Models;

namespace Nexo.Core.Application.Trust.Ports;

/// <summary>
/// User-facing knowledge log: what Nexo has learned about the user
/// (preferences, patterns, workflow habits). For trust and audit; not for sync.
/// </summary>
public interface IUserKnowledgeLogStore
{
    /// <summary>Add or update an entry. Updates create new versions.</summary>
    Task UpsertAsync(UserKnowledgeLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Soft-delete an entry by id.</summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Get an entry by id (excluding deleted).</summary>
    Task<UserKnowledgeLogEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Get entries, optionally filtered by data type. Excludes deleted. Newest first.</summary>
    Task<IReadOnlyList<UserKnowledgeLogEntry>> GetAsync(string? dataType = null, int maxCount = 100, CancellationToken cancellationToken = default);

    /// <summary>Export to JSON string including provenance.</summary>
    Task<string> ExportToJsonAsync(int maxCount = 1000, CancellationToken cancellationToken = default);

    /// <summary>Export to Markdown string including provenance.</summary>
    Task<string> ExportToMarkdownAsync(int maxCount = 1000, CancellationToken cancellationToken = default);
}
