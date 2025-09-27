using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NexoDoomGame.ExternalGeneration
{
    /// <summary>
    /// Base class generation functionality for redundancy-free Nexo script generator
    /// </summary>
    public partial class RedundancyFreeNexoGenerator
    {
        private async Task GenerateBaseClasses()
        {
            Console.WriteLine("🏗️ Generating base classes and interfaces...");
            
            var baseDir = "GeneratedNexoScripts/Base";
            Directory.CreateDirectory(baseDir);
            
            // Generate base domain logic class
            var baseDomainLogic = GenerateBaseDomainLogicClass();
            await File.WriteAllTextAsync(Path.Combine(baseDir, "BaseDomainLogic.cs"), baseDomainLogic);
            
            // Generate base platform implementation class
            var basePlatformImpl = GenerateBasePlatformImplementationClass();
            await File.WriteAllTextAsync(Path.Combine(baseDir, "BasePlatformImplementation.cs"), basePlatformImpl);
            
            // Generate base composition class
            var baseComposition = GenerateBaseCompositionClass();
            await File.WriteAllTextAsync(Path.Combine(baseDir, "BaseCompositionComponent.cs"), baseComposition);
            
            Console.WriteLine("✅ Base classes generated");
        }
        
        private string GenerateBaseDomainLogicClass()
        {
            return @"using System;
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
    public interface IBaseDomainLogicProvider
    {
        Task<BaseDomainLogic> CreateAsync();
        Task<bool> ValidateAsync();
        Task<object> ExecuteAsync(object input);
    }
    
    /// <summary>
    /// Base interface for domain logic validation
    /// </summary>
    public interface IBaseDomainLogicValidator
    {
        Task<bool> ValidateAsync(BaseDomainLogic component);
        Task<string[]> GetValidationErrorsAsync(BaseDomainLogic component);
    }
}";
        }
        
        private string GenerateBasePlatformImplementationClass()
        {
            return @"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexoDoomGame.DomainLogic.Base;

namespace NexoDoomGame.PlatformImplementations.Base
{
    /// <summary>
    /// Base class for all platform implementations
    /// Provides common platform-specific functionality
    /// </summary>
    public abstract class BasePlatformImplementation : BaseDomainLogic
    {
        protected readonly string PlatformName;
        protected readonly string TargetFramework;
        protected readonly string ImplementationStyle;
        
        protected BasePlatformImplementation(string platformName, string targetFramework, string implementationStyle)
        {
            PlatformName = platformName;
            TargetFramework = targetFramework;
            ImplementationStyle = implementationStyle;
        }
        
        /// <summary>
        /// Get platform-specific information
        /// </summary>
        public virtual Dictionary<string, object> GetPlatformInfo()
        {
            return new Dictionary<string, object>
            {
                [""Platform""] = PlatformName,
                [""Framework""] = TargetFramework,
                [""Style""] = ImplementationStyle
            };
        }
        
        /// <summary>
        /// Platform-specific initialization
        /// </summary>
        public override async Task InitializeAsync()
        {
            // Platform-specific initialization logic
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Platform-specific cleanup
        /// </summary>
        public override async Task CleanupAsync()
        {
            // Platform-specific cleanup logic
            await Task.CompletedTask;
        }
    }
}";
        }
        
        private string GenerateBaseCompositionClass()
        {
            return @"using System;
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
            PlatformMappings[$""{domain}.{platform}""] = platform;
        }
        
        /// <summary>
        /// Get platform-specific implementation
        /// </summary>
        public virtual T GetPlatformImplementation<T>(string domain, string platform) where T : class
        {
            var key = $""{domain}.{platform}"";
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
            return new { Component = component.GetType().Name, Status = ""Executed"" };
        }
    }
}";
        }
    }
}
