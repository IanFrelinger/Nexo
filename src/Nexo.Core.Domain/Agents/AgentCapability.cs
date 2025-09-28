using Nexo.Core.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Agents
{
    /// <summary>
    /// Represents a capability or skill that an agent possesses
    /// </summary>
    public sealed class AgentCapability : IEquatable<AgentCapability>
    {
        public string Name { get; }
        public string Description { get; }
        public string Category { get; }
        public int ProficiencyLevel { get; }
        public IReadOnlyList<string> RequiredResources { get; }
        public IReadOnlyList<string> SupportedPlatforms { get; }

        public AgentCapability(
            string name, 
            string description, 
            string category, 
            int proficiencyLevel = 1,
            IEnumerable<string>? requiredResources = null,
            IEnumerable<string>? supportedPlatforms = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Category = category ?? throw new ArgumentNullException(nameof(category));
            ProficiencyLevel = Math.Max(1, Math.Min(10, proficiencyLevel));
            RequiredResources = (requiredResources?.ToList() ?? new List<string>()).AsReadOnly();
            SupportedPlatforms = (supportedPlatforms?.ToList() ?? new List<string>()).AsReadOnly();
        }

        // Common capability categories
        public static class Categories
        {
            public const string AI = "AI";
            public const string Development = "Development";
            public const string Analysis = "Analysis";
            public const string Communication = "Communication";
            public const string Security = "Security";
            public const string Infrastructure = "Infrastructure";
            public const string Data = "Data";
            public const string UserInterface = "UserInterface";
        }

        // Common capabilities
        public static readonly AgentCapability CodeGeneration = new(
            "Code Generation", 
            "Generate code in various programming languages", 
            Categories.Development, 
            8,
            new[] { "CPU", "Memory" },
            new[] { "Windows", "macOS", "Linux", "Web" });

        public static readonly AgentCapability CodeAnalysis = new(
            "Code Analysis", 
            "Analyze and understand code structure and patterns", 
            Categories.Analysis, 
            9,
            new[] { "CPU", "Memory" },
            new[] { "Windows", "macOS", "Linux", "Web" });

        public static readonly AgentCapability NaturalLanguageProcessing = new(
            "Natural Language Processing", 
            "Process and understand human language", 
            Categories.AI, 
            7,
            new[] { "CPU", "Memory", "GPU" },
            new[] { "Windows", "macOS", "Linux", "Web", "Cloud" });

        public static readonly AgentCapability CrossPlatformDeployment = new(
            "Cross-Platform Deployment", 
            "Deploy applications across multiple platforms", 
            Categories.Infrastructure, 
            6,
            new[] { "CPU", "Memory", "Network" },
            new[] { "Windows", "macOS", "Linux", "Cloud", "Container" });

        public static readonly AgentCapability SecurityAnalysis = new(
            "Security Analysis", 
            "Analyze code and systems for security vulnerabilities", 
            Categories.Security, 
            8,
            new[] { "CPU", "Memory" },
            new[] { "Windows", "macOS", "Linux", "Web" });

        public static readonly AgentCapability DataProcessing = new(
            "Data Processing", 
            "Process and analyze large datasets", 
            Categories.Data, 
            7,
            new[] { "CPU", "Memory", "Storage" },
            new[] { "Windows", "macOS", "Linux", "Cloud" });

        public static readonly AgentCapability UserInterfaceDesign = new(
            "User Interface Design", 
            "Design and implement user interfaces", 
            Categories.UserInterface, 
            6,
            new[] { "CPU", "Memory", "GPU" },
            new[] { "Windows", "macOS", "Linux", "Web", "Mobile" });

        public static readonly AgentCapability AgentCommunication = new(
            "Agent Communication", 
            "Communicate and coordinate with other agents", 
            Categories.Communication, 
            9,
            new[] { "CPU", "Memory", "Network" },
            new[] { "Windows", "macOS", "Linux", "Web", "Cloud", "Container" });

        public static readonly AgentCapability AssemblyAnalysis = new(
            "Assembly Analysis", 
            "Analyze .NET assemblies for metadata, dependencies, and structure", 
            Categories.Analysis, 
            8,
            new[] { "CPU", "Memory", "Storage" },
            new[] { "Windows", "macOS", "Linux" });

        public static readonly AgentCapability AssemblyDecompilation = new(
            "Assembly Decompilation", 
            "Decompile .NET assemblies to source code and IL", 
            Categories.Analysis, 
            9,
            new[] { "CPU", "Memory", "Storage" },
            new[] { "Windows", "macOS", "Linux" });

        public static readonly AgentCapability ILAnalysis = new(
            "IL Analysis", 
            "Analyze Intermediate Language code for patterns and issues", 
            Categories.Analysis, 
            7,
            new[] { "CPU", "Memory" },
            new[] { "Windows", "macOS", "Linux" });

        public static readonly AgentCapability AssemblySecurityScanning = new(
            "Assembly Security Scanning", 
            "Scan assemblies for security vulnerabilities and threats", 
            Categories.Security, 
            8,
            new[] { "CPU", "Memory" },
            new[] { "Windows", "macOS", "Linux" });

        public static readonly AgentCapability AssemblyPerformanceAnalysis = new(
            "Assembly Performance Analysis", 
            "Analyze assembly performance characteristics and bottlenecks", 
            Categories.Analysis, 
            7,
            new[] { "CPU", "Memory" },
            new[] { "Windows", "macOS", "Linux" });

        // Collection of all predefined capabilities
        public static readonly IReadOnlyList<AgentCapability> All = new[]
        {
            CodeGeneration, CodeAnalysis, NaturalLanguageProcessing, CrossPlatformDeployment,
            SecurityAnalysis, DataProcessing, UserInterfaceDesign, AgentCommunication,
            AssemblyAnalysis, AssemblyDecompilation, ILAnalysis, AssemblySecurityScanning, AssemblyPerformanceAnalysis
        };

        // Factory methods
        public static AgentCapability FromName(string name) =>
            All.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
            new AgentCapability(name, "Custom capability", "Custom", 1);

        public static AgentCapability CreateCustom(string name, string description, string category, int proficiencyLevel = 1) =>
            new AgentCapability(name, description, category, proficiencyLevel);

        // Equality and comparison
        public bool Equals(AgentCapability? other) => 
            other != null && Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) => obj is AgentCapability other && Equals(other);
        public override int GetHashCode() => Name.GetHashCode(StringComparison.OrdinalIgnoreCase);
        public override string ToString() => $"{Name} (Level {ProficiencyLevel})";

        public static bool operator ==(AgentCapability? left, AgentCapability? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(AgentCapability? left, AgentCapability? right) => 
            !(left == right);
    }
}
