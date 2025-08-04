using Nexo.Feature.Data.Enums;
using Nexo.Feature.Data.Models;

namespace Nexo.Feature.Data.Interfaces
{
    /// <summary>
    /// Interface for tracking entity changes in the database context
    /// </summary>
    public interface IChangeTracker
    {
        /// <summary>
        /// Gets all entities being tracked
        /// </summary>
        /// <returns>Collection of tracked entities</returns>
        IEnumerable<object> GetTrackedEntities();

        /// <summary>
        /// Gets entities with the specified state
        /// </summary>
        /// <param name="state">The entity state</param>
        /// <returns>Collection of entities in the specified state</returns>
        IEnumerable<object> GetEntitiesWithState(EntityState state);

        /// <summary>
        /// Gets the state of an entity
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>The entity state</returns>
        EntityState GetEntityState(object entity);

        /// <summary>
        /// Checks if an entity is being tracked
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>True if the entity is being tracked</returns>
        bool IsTracked(object entity);

        /// <summary>
        /// Detaches an entity from tracking
        /// </summary>
        /// <param name="entity">The entity to detach</param>
        void Detach(object entity);

        /// <summary>
        /// Gets the number of entities being tracked
        /// </summary>
        /// <returns>Number of tracked entities</returns>
        int GetTrackedEntitiesCount();

        /// <summary>
        /// Gets a summary of changes
        /// </summary>
        /// <returns>Change summary</returns>
        ChangeSummary GetChangeSummary();
    }
} 