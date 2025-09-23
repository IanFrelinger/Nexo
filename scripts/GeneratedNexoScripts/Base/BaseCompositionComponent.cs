using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexoDoomGame.DomainLogic.Base;

namespace NexoDoomGame.Composition.Base
{
    /// <summary>
    /// Base class for composition components
    /// Provides common orchestration functionality
    /// </summary>
    public abstract class BaseCompositionComponent
    {
        protected readonly Dictionary<string, object> Components = new();
        protected readonly Dictionary<string, string> PlatformMappings = new();
        
        /// <summary>
        /// Register a component
        /// </summary>
        public virtual void RegisterComponent<T>(string name, T component) where T : class
        {
            Components[name] = component;
        }
        
        /// <summary>
        /// Get a component by name
        /// </summary>
        public virtual T GetComponent<T>(string name) where T : class
        {
            return Components.TryGetValue(name, out var component) ? component as T : null;
        }
        
        /// <summary>
        /// Register platform-specific implementation
        /// </summary>
        public virtual void RegisterPlatformImplementation(string domain, string platform, object implementation)
        {
            PlatformMappings[$"{domain}.{platform}"] = platform;
        }
        
        /// <summary>
        /// Get platform-specific implementation
        /// </summary>
        public virtual T GetPlatformImplementation<T>(string domain, string platform) where T : class
        {
            var key = $"{domain}.{platform}";
            return PlatformMappings.TryGetValue(key, out var platformName) ? GetComponent<T>(platformName) : null;
        }
        
        /// <summary>
        /// Execute component logic
        /// </summary>
        protected virtual async Task<object> ExecuteComponentAsync(object component)
        {
            if (component is BaseDomainLogic domainLogic)
            {
                return await domainLogic.ExecuteAsync(null);
            }
            return new { Component = component.GetType().Name, Status = "Executed" };
        }
    }
}