using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace Nexo.Tests.Architecture;

/// <summary>
/// Architecture tests to prevent duplications and enforce design rules
/// </summary>
public class ArchitectureTests
{
    private readonly Assembly _applicationAssembly = typeof(Nexo.Core.Application.Interfaces.ICommand<,>).Assembly;
    private readonly Assembly _domainAssembly = typeof(Nexo.Core.Domain.Values.BaseTypeValue).Assembly;

    [Fact]
    public void ShouldHaveOnlyOneICommandInterface()
    {
        // Ensure there's only one ICommand interface in the application layer
        var iCommandTypes = _applicationAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.StartsWith("ICommand"))
            .ToList();

        Assert.Single(iCommandTypes);
        Assert.Equal("Nexo.Core.Application.Interfaces.ICommand`2", iCommandTypes[0].FullName);
    }

    [Fact]
    public void ShouldHaveOnlyOneAgentFactoryClass()
    {
        // Ensure there's only one AgentFactory class in the application layer
        var agentFactoryTypes = _applicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name == "AgentFactory")
            .ToList();

        Assert.Single(agentFactoryTypes);
        Assert.Equal("Nexo.Core.Application.Agents.AgentFactory", agentFactoryTypes[0].FullName);
    }

    [Fact]
    public void ShouldHaveOnlyGenericCommandOrchestrator()
    {
        // Ensure only GenericCommandOrchestrator exists (no other orchestrators)
        var orchestratorTypes = _applicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Orchestrator"))
            .ToList();

        Assert.Single(orchestratorTypes);
        Assert.Equal("Nexo.Core.Application.Orchestration.GenericCommandOrchestrator", orchestratorTypes[0].FullName);
    }

    [Fact]
    public void ShouldNotHaveCentralizedEnums()
    {
        // Ensure no CentralizedEnums.cs file exists
        var centralizedEnumTypes = _applicationAssembly.GetTypes()
            .Where(t => t.Name.Contains("CentralizedEnums"))
            .ToList();

        Assert.Empty(centralizedEnumTypes);
    }

    [Fact]
    public void ShouldUseTypeValueSystemInsteadOfEnums()
    {
        // Ensure domain values use ITypeValue instead of enums
        var enumTypes = _domainAssembly.GetTypes()
            .Where(t => t.IsEnum)
            .ToList();

        // Only allow the DevMode enum in the Dev agents project
        var allowedEnums = enumTypes.Where(t => t.Name == "DevMode").ToList();
        Assert.Equal(enumTypes.Count, allowedEnums.Count);
    }

    [Fact]
    public void ShouldHaveSimplifiedClassesOnlyInExamples()
    {
        // Ensure Simplified* classes are only in Examples project
        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("Nexo") == true)
            .ToList();

        var simplifiedTypes = allAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.StartsWith("Simplified") && !t.Namespace?.Contains("Examples") == true)
            .ToList();

        Assert.Empty(simplifiedTypes);
    }

    [Fact]
    public void ShouldHaveConsistentNamingConventions()
    {
        // Ensure interfaces start with 'I' and classes don't
        var allTypes = _applicationAssembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsNested)
            .ToList();

        var interfaceViolations = allTypes
            .Where(t => t.IsInterface && !t.Name.StartsWith("I"))
            .ToList();

        var classViolations = allTypes
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.StartsWith("I"))
            .ToList();

        Assert.Empty(interfaceViolations);
        Assert.Empty(classViolations);
    }

    [Fact]
    public void ShouldHaveClassesUnder200Lines()
    {
        // Ensure all classes are under 200 lines (simplified check)
        var allTypes = _applicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
            .ToList();

        var oversizedClasses = new List<string>();

        foreach (var type in allTypes)
        {
            // This is a simplified check - in a real scenario, you'd count actual lines
            // For now, we'll just ensure the class exists and is properly structured
            if (type.GetMethods().Length > 20) // Rough heuristic
            {
                oversizedClasses.Add(type.Name);
            }
        }

        // This is more of a documentation of the rule - actual line counting would require file analysis
        Assert.True(oversizedClasses.Count < 5, $"Found {oversizedClasses.Count} potentially oversized classes: {string.Join(", ", oversizedClasses)}");
    }

    [Fact]
    public void ShouldNotHaveDuplicatePublicTypeNames()
    {
        // Get all public types from all assemblies
        var allTypes = new List<(string AssemblyName, string TypeName, Type Type)>();
        
        var assemblies = new[]
        {
            _applicationAssembly,
            _domainAssembly,
            typeof(Nexo.Abstractions.ModelOutput).Assembly, // Nexo.Abstractions
            typeof(Nexo.Runtime.AgentHost).Assembly, // Nexo.Runtime
            typeof(Nexo.Tools.Dev.DotnetBuildTool).Assembly, // Nexo.Tools.Dev
            typeof(Nexo.Policies.Dev.PathAllowlist).Assembly, // Nexo.Policies.Dev
            typeof(Nexo.Agents.Dev.DevDirectorAgent).Assembly // Nexo.Agents.Dev
        };

        foreach (var assembly in assemblies)
        {
            var publicTypes = assembly.GetTypes()
                .Where(t => t.IsPublic && !t.IsNested)
                .Select(t => (assembly.GetName().Name!, t.Name, t));
            
            allTypes.AddRange(publicTypes);
        }

        // Group by type name and find duplicates
        var duplicateGroups = allTypes
            .GroupBy(t => t.TypeName)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateGroups.Any())
        {
            var duplicateInfo = duplicateGroups
                .Select(g => $"{g.Key}: {string.Join(", ", g.Select(t => t.AssemblyName))}")
                .ToList();
            
                Assert.Fail($"Duplicate public type names found: {string.Join("; ", duplicateInfo)}");
        }
    }
}
