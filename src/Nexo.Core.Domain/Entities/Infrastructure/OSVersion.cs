using System;

namespace Nexo.Core.Domain.Entities.Infrastructure
{
    /// <summary>
    /// Operating system version information
    /// </summary>
    public class OSVersion
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
        
        /// <summary>
        /// Operating system service pack
        /// </summary>
        public string ServicePack { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system edition
        /// </summary>
        public string Edition { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system language
        /// </summary>
        public string Language { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system region
        /// </summary>
        public string Region { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system time zone
        /// </summary>
        public string TimeZone { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system locale
        /// </summary>
        public string Locale { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system culture
        /// </summary>
        public string Culture { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system display name
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system description
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system manufacturer
        /// </summary>
        public string Manufacturer { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system product name
        /// </summary>
        public string ProductName { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system product type
        /// </summary>
        public string ProductType { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system product version
        /// </summary>
        public string ProductVersion { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system serial number
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;
        
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
        
        /// <summary>
        /// Operating system processor stepping identifier
        /// </summary>
        public string SteppingIdentifier { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor family identifier
        /// </summary>
        public string FamilyIdentifier { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor model identifier
        /// </summary>
        public string ModelIdentifier { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor brand identifier
        /// </summary>
        public string BrandIdentifier { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor signature identifier
        /// </summary>
        public string SignatureIdentifier { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor stepping identifier
        /// </summary>
        public string SteppingIdentifier2 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor family identifier
        /// </summary>
        public string FamilyIdentifier2 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor model identifier
        /// </summary>
        public string ModelIdentifier2 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor brand identifier
        /// </summary>
        public string BrandIdentifier2 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor signature identifier
        /// </summary>
        public string SignatureIdentifier2 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor stepping identifier
        /// </summary>
        public string SteppingIdentifier3 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor family identifier
        /// </summary>
        public string FamilyIdentifier3 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor model identifier
        /// </summary>
        public string ModelIdentifier3 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor brand identifier
        /// </summary>
        public string BrandIdentifier3 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor signature identifier
        /// </summary>
        public string SignatureIdentifier3 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor stepping identifier
        /// </summary>
        public string SteppingIdentifier4 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor family identifier
        /// </summary>
        public string FamilyIdentifier4 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor model identifier
        /// </summary>
        public string ModelIdentifier4 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor brand identifier
        /// </summary>
        public string BrandIdentifier4 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor signature identifier
        /// </summary>
        public string SignatureIdentifier4 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor stepping identifier
        /// </summary>
        public string SteppingIdentifier5 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor family identifier
        /// </summary>
        public string FamilyIdentifier5 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor model identifier
        /// </summary>
        public string ModelIdentifier5 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor brand identifier
        /// </summary>
        public string BrandIdentifier5 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor signature identifier
        /// </summary>
        public string SignatureIdentifier5 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor stepping identifier
        /// </summary>
        public string SteppingIdentifier6 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor family identifier
        /// </summary>
        public string FamilyIdentifier6 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor model identifier
        /// </summary>
        public string ModelIdentifier6 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor brand identifier
        /// </summary>
        public string BrandIdentifier6 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor signature identifier
        /// </summary>
        public string SignatureIdentifier6 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor stepping identifier
        /// </summary>
        public string SteppingIdentifier7 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor family identifier
        /// </summary>
        public string FamilyIdentifier7 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor model identifier
        /// </summary>
        public string ModelIdentifier7 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor brand identifier
        /// </summary>
        public string BrandIdentifier7 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor signature identifier
        /// </summary>
        public string SignatureIdentifier7 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor stepping identifier
        /// </summary>
        public string SteppingIdentifier8 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor family identifier
        /// </summary>
        public string FamilyIdentifier8 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor model identifier
        /// </summary>
        public string ModelIdentifier8 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor brand identifier
        /// </summary>
        public string BrandIdentifier8 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor signature identifier
        /// </summary>
        public string SignatureIdentifier8 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor stepping identifier
        /// </summary>
        public string SteppingIdentifier9 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor family identifier
        /// </summary>
        public string FamilyIdentifier9 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor model identifier
        /// </summary>
        public string ModelIdentifier9 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor brand identifier
        /// </summary>
        public string BrandIdentifier9 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor signature identifier
        /// </summary>
        public string SignatureIdentifier9 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor stepping identifier
        /// </summary>
        public string SteppingIdentifier10 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor family identifier
        /// </summary>
        public string FamilyIdentifier10 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor model identifier
        /// </summary>
        public string ModelIdentifier10 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor brand identifier
        /// </summary>
        public string BrandIdentifier10 { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system processor signature identifier
        /// </summary>
        public string SignatureIdentifier10 { get; set; } = string.Empty;
    }
}
