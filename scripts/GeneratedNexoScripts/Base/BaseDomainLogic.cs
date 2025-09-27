using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NexoDoomGame.DomainLogic.Base
{
    /// <summary>
    /// Base class for all domain logic components
    /// Provides common functionality and enforces consistent patterns
    /// </summary>
    public abstract class BaseDomainLogic
    {
        /// <summary>
        /// Validate the component state and configuration
        /// </summary>
        public abstract Task<bool> ValidateAsync();
        
        /// <summary>
        /// Execute the core domain logic
        /// </summary>
        public abstract Task<object> ExecuteAsync(object input);
        
        /// <summary>
        /// Get the current state of the component
        /// </summary>
        public abstract Task<Dictionary<string, object>> GetStateAsync();
        
        /// <summary>
        /// Initialize the component
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Cleanup resources
        /// </summary>
        public virtual async Task CleanupAsync()
        {
            await Task.CompletedTask;
        }
    }
    
    /// <summary>
    /// Base interface for domain logic providers
    /// </summary>
    public partial interface IBaseDomainLogicProvider
    {
        Task<BaseDomainLogic> CreateAsync();
        Task<bool> ValidateAsync();
        Task<object> ExecuteAsync(object input);
    }
    
    /// <summary>
    /// Base interface for domain logic validation
    /// </summary>
    public partial interface IBaseDomainLogicValidator
    {
        Task<bool> ValidateAsync(BaseDomainLogic component);
        Task<string[]> GetValidationErrorsAsync(BaseDomainLogic component);
    }
}