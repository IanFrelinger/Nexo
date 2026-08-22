namespace Ashlar.Core.Application.Persistence.Models;

/// <summary>
/// Optional marker for entities that have a stable key. Used by repository
/// implementations to support Update and key-based lookup. Entities that do not
/// implement this can still use repositories if the implementation supports
/// key extraction (e.g. via configuration or convention).
/// </summary>
/// <typeparam name="TKey">Type of the entity's key (e.g. int, long, Guid, string).</typeparam>
public interface IEntity<out TKey>
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    TKey Id { get; }
}
