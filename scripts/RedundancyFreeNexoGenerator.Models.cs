using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NexoDoomGame.ExternalGeneration
{
    /// <summary>
    /// Model classes for redundancy-free Nexo script generator
    /// </summary>
    public partial class RedundancyFreeNexoGenerator
    {
        // Model classes are defined here for the redundancy-free Nexo generator
    }
    
    // Configuration classes for V2
    public class ScriptGenerationConfigV2
    {
        public ConfigurationDefaults Defaults { get; set; } = new();
        public DomainLogicComponent[] DomainLogicComponents { get; set; } = Array.Empty<DomainLogicComponent>();
        public PlatformImplementation[] PlatformImplementations { get; set; } = Array.Empty<PlatformImplementation>();
        public CompositionComponent[] CompositionComponents { get; set; } = Array.Empty<CompositionComponent>();
    }
    
    public class ConfigurationDefaults
    {
        public string[] CrossDomainUsages { get; set; } = Array.Empty<string>();
        public string[] CommonDependencies { get; set; } = Array.Empty<string>();
        public string[] CommonResponsibilities { get; set; } = Array.Empty<string>();
    }
    
    // Reuse existing component classes
    public class DomainLogicComponent
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Domain { get; set; } = "";
        public string[] Interfaces { get; set; } = Array.Empty<string>();
        public string[] Dependencies { get; set; } = Array.Empty<string>();
        public string[] CrossDomainUsages { get; set; } = Array.Empty<string>();
    }
    
    public class PlatformImplementation
    {
        public string Platform { get; set; } = "";
        public string TargetFramework { get; set; } = "";
        public string ImplementationStyle { get; set; } = "";
        public string[] Dependencies { get; set; } = Array.Empty<string>();
    }
    
    public class CompositionComponent
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string[] Responsibilities { get; set; } = Array.Empty<string>();
    }
}
