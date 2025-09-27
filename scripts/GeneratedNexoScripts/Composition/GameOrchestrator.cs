using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexoDoomGame.DomainLogic;
using NexoDoomGame.Composition.Base;

namespace NexoDoomGame.Composition
{
    /// <summary>
    /// Orchestrates all domain logic components and manages their lifecycle
    /// Responsibilities: Component Registration, Dependency Injection, Lifecycle Management, Cross-Component Communication, Error Handling and Recovery
    /// </summary>
    public partial class GameOrchestrator : BaseCompositionComponent
    {
        /// <summary>
        /// Orchestrate all registered components
        /// </summary>
        public async Task<Dictionary<string, object>> OrchestrateAsync()
        {
            var results = new Dictionary<string, object>();
            
            foreach (var component in Components)
            {
                if (component.Value is MovementLogic or CombatLogic or HealthLogic or AILogic or AudioLogic)
                {
                    results[component.Key] = await ExecuteComponentAsync(component.Value);
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Initialize all registered components
        /// </summary>
        public async Task InitializeAllAsync()
        {
            foreach (var component in Components.Values)
            {
                if (component is BaseDomainLogic domainLogic)
                {
                    await domainLogic.InitializeAsync();
                }
            }
        }
        
        /// <summary>
        /// Cleanup all registered components
        /// </summary>
        public async Task CleanupAllAsync()
        {
            foreach (var component in Components.Values)
            {
                if (component is BaseDomainLogic domainLogic)
                {
                    await domainLogic.CleanupAsync();
                }
            }
        }
    }
}