using System;
using System.Collections.Generic;

namespace Nexo.Shared.Composable.CommandTypes
{
    /// <summary>
    /// Interface for command capabilities
    /// </summary>
    public interface ICommandCapabilities
    {
        IReadOnlyList<ICapability> ProvidedCapabilities { get; }
        IReadOnlyList<ICapability> RequiredCapabilities { get; }
        IReadOnlyList<IResourceRequirement> ResourceRequirements { get; }
        IReadOnlyList<IPlatformSupport> SupportedPlatforms { get; }
    }
    
    /// <summary>
    /// Interface for capabilities
    /// </summary>
    public interface ICapability
    {
        string Name { get; }
        string Description { get; }
        string Category { get; }
        int Level { get; }
    }
    
    /// <summary>
    /// Interface for resource requirements
    /// </summary>
    public interface IResourceRequirement
    {
        string ResourceType { get; }
        string Description { get; }
        int MinimumAmount { get; }
        int MaximumAmount { get; }
        string Unit { get; }
    }
    
    /// <summary>
    /// Interface for platform support
    /// </summary>
    public interface IPlatformSupport
    {
        string PlatformName { get; }
        string Version { get; }
        bool IsSupported { get; }
        string[] RequiredFeatures { get; }
    }
    
    /// <summary>
    /// Default implementation of command capabilities
    /// </summary>
    public class CommandCapabilities : ICommandCapabilities
    {
        public IReadOnlyList<ICapability> ProvidedCapabilities { get; }
        public IReadOnlyList<ICapability> RequiredCapabilities { get; }
        public IReadOnlyList<IResourceRequirement> ResourceRequirements { get; }
        public IReadOnlyList<IPlatformSupport> SupportedPlatforms { get; }
        
        public CommandCapabilities(
            IReadOnlyList<ICapability>? providedCapabilities = null,
            IReadOnlyList<ICapability>? requiredCapabilities = null,
            IReadOnlyList<IResourceRequirement>? resourceRequirements = null,
            IReadOnlyList<IPlatformSupport>? supportedPlatforms = null)
        {
            ProvidedCapabilities = providedCapabilities ?? new List<ICapability>();
            RequiredCapabilities = requiredCapabilities ?? new List<ICapability>();
            ResourceRequirements = resourceRequirements ?? new List<IResourceRequirement>();
            SupportedPlatforms = supportedPlatforms ?? new List<IPlatformSupport>();
        }
        
        public static ICommandCapabilities None() => new CommandCapabilities();
        
        public static ICommandCapabilities WithCapabilities(params ICapability[] capabilities) => 
            new CommandCapabilities(providedCapabilities: capabilities);
    }
}
