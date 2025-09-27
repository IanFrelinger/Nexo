using System;

namespace Nexo.Core.Domain.Entities.Infrastructure
{
    /// <summary>
    /// Operating system version information.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class OSVersion
    {
        /// <summary>
        /// Operating system name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system version
        /// </summary>
        public string Version { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system build number
        /// </summary>
        public string Build { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system architecture
        /// </summary>
        public string Architecture { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether the operating system is 64-bit
        /// </summary>
        public bool Is64Bit { get; set; }
        // This class acts as an orchestrator for various OS version functionalities,
        // with specific categories defined in partial classes.
    }
}