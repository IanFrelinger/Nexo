using System;

namespace Nexo.Core.Domain.Entities.Infrastructure
{
    /// <summary>
    /// System information
    /// </summary>
    public partial class OSVersion
    {
        /// <summary>
        /// Operating system installation date
        /// </summary>
        public DateTime InstallationDate { get; set; }
        
        /// <summary>
        /// Operating system last boot time
        /// </summary>
        public DateTime LastBootTime { get; set; }
        
        /// <summary>
        /// Operating system uptime
        /// </summary>
        public TimeSpan Uptime { get; set; }
        
        /// <summary>
        /// Operating system total memory
        /// </summary>
        public long TotalMemory { get; set; }
        
        /// <summary>
        /// Operating system available memory
        /// </summary>
        public long AvailableMemory { get; set; }
        
        /// <summary>
        /// Operating system processor count
        /// </summary>
        public int ProcessorCount { get; set; }
        
        /// <summary>
        /// Operating system processor architecture
        /// </summary>
        public string ProcessorArchitecture { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor type
        /// </summary>
        public string ProcessorType { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor level
        /// </summary>
        public int ProcessorLevel { get; set; }
        
        /// <summary>
        /// Operating system processor revision
        /// </summary>
        public int ProcessorRevision { get; set; }
    }
}
