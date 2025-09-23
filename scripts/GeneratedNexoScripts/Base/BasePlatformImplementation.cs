using System;
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
                ["Platform"] = PlatformName,
                ["Framework"] = TargetFramework,
                ["Style"] = ImplementationStyle
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
}