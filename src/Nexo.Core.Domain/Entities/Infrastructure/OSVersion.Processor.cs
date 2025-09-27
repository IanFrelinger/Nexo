using System;

namespace Nexo.Core.Domain.Entities.Infrastructure
{
    /// <summary>
    /// Processor information
    /// </summary>
    public partial class OSVersion
    {
        /// <summary>
        /// Operating system processor identifier
        /// </summary>
        public string ProcessorIdentifier { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor name
        /// </summary>
        public string ProcessorName { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor vendor
        /// </summary>
        public string ProcessorVendor { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor frequency
        /// </summary>
        public long ProcessorFrequency { get; set; }
        
        /// <summary>
        /// Operating system processor cache size
        /// </summary>
        public long ProcessorCacheSize { get; set; }
        
        /// <summary>
        /// Operating system processor cores
        /// </summary>
        public int ProcessorCores { get; set; }
        
        /// <summary>
        /// Operating system processor logical processors
        /// </summary>
        public int LogicalProcessors { get; set; }
        
        /// <summary>
        /// Operating system processor physical processors
        /// </summary>
        public int PhysicalProcessors { get; set; }
        
        /// <summary>
        /// Operating system processor hyperthreading enabled
        /// </summary>
        public bool HyperthreadingEnabled { get; set; }
        
        /// <summary>
        /// Operating system processor virtualization enabled
        /// </summary>
        public bool VirtualizationEnabled { get; set; }
        
        /// <summary>
        /// Operating system processor security features
        /// </summary>
        public string SecurityFeatures { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor extensions
        /// </summary>
        public string Extensions { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor capabilities
        /// </summary>
        public string Capabilities { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor features
        /// </summary>
        public string Features { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor instructions
        /// </summary>
        public string Instructions { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor microcode
        /// </summary>
        public string Microcode { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor stepping
        /// </summary>
        public string Stepping { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor family
        /// </summary>
        public string Family { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor model
        /// </summary>
        public string Model { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor brand
        /// </summary>
        public string Brand { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor signature
        /// </summary>
        public string Signature { get; set; } = string.Empty;
    }
}
